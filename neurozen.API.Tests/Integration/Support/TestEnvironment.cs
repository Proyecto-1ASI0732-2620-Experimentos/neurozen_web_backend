using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using neurozen.API.IAM.Infrastructure.Seeding;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using Testcontainers.MySql;

namespace neurozen.API.Tests.Integration.Support;

/// <summary>
///     One API instance and one fresh MySQL database per test run, shared by the integration tests and the BDD
///     scenarios. MySQL comes from NEUROZEN_TEST_MYSQL (server-level connection string, as in CI) or, when it is not
///     set, from a Testcontainers mysql:8.0 container (requires Docker).
/// </summary>
public sealed class TestEnvironment
{
    public const string JwtSecret = "clave-de-pruebas-de-integracion-0123456789-abcdef";
    public const string JwtIssuer = "neurozen-api";
    public const string JwtAudience = "neurozen-web";
    public const string AdminEmail = "admin@neurozen.com";
    public const string AdminPassword = "Admin2026";

    private static readonly SemaphoreSlim InitializationLock = new(1, 1);
    private static TestEnvironment? _current;
    private static MySqlContainer? _container;

    private TestEnvironment(NeuroZenApiFactory factory, string databaseConnectionString)
    {
        Factory = factory;
        DatabaseConnectionString = databaseConnectionString;
        Api = new TestApi(factory);
    }

    public NeuroZenApiFactory Factory { get; }
    public TestApi Api { get; }
    public string DatabaseConnectionString { get; }

    public static async Task<TestEnvironment> GetAsync()
    {
        if (_current is not null) return _current;

        await InitializationLock.WaitAsync();
        try
        {
            if (_current is not null) return _current;

            var serverConnectionString = await ResolveServerConnectionStringAsync();
            var database = new MySqlConnectionStringBuilder(serverConnectionString)
            {
                Database = ("neurozen_tests_" + Guid.NewGuid().ToString("N"))[..30]
            }.ConnectionString;

            // Read by Program.cs at startup (environment variables are visible before the host is built).
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", database);
            Environment.SetEnvironmentVariable("Database__ServerVersion", "8.0.36-mysql");
            Environment.SetEnvironmentVariable("TokenSettings__Secret", JwtSecret);
            Environment.SetEnvironmentVariable("TokenSettings__Issuer", JwtIssuer);
            Environment.SetEnvironmentVariable("TokenSettings__Audience", JwtAudience);
            Environment.SetEnvironmentVariable("Seed__Admin__Email", AdminEmail);
            Environment.SetEnvironmentVariable("Seed__Admin__Password", AdminPassword);
            Environment.SetEnvironmentVariable("Seed__Admin__FullName", "Administrador NeuroZen");
            Environment.SetEnvironmentVariable("Seed__Demo__Enabled", "false");
            Environment.SetEnvironmentVariable("PasswordReset__FrontendBaseUrl", "http://localhost:5173");

            var factory = new NeuroZenApiFactory();
            _ = factory.Server; // starts the host: Program.cs migrates and seeds when migrations exist

            await EnsureSchemaAsync(factory);

            _current = new TestEnvironment(factory, database);
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Cleanup();
            return _current;
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    /// <summary>Runs an action against the test database (assertions and time travel in flows).</summary>
    public async Task WithDatabaseAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public async Task<T> QueryDatabaseAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = Factory.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private static async Task<string> ResolveServerConnectionStringAsync()
    {
        var configured = Environment.GetEnvironmentVariable("NEUROZEN_TEST_MYSQL");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        try
        {
            _container = new MySqlBuilder()
                .WithImage("mysql:8.0")
                .WithUsername("root")
                .WithPassword("root")
                .Build();
            await _container.StartAsync();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Las pruebas de integración y BDD necesitan MySQL 8 y no hay ninguno disponible. " +
                "Opción 1: abre Docker Desktop y espera a que diga «Engine running» (Testcontainers levanta MySQL solo). " +
                "Opción 2: usa un MySQL 8 propio con la variable NEUROZEN_TEST_MYSQL, por ejemplo " +
                "$env:NEUROZEN_TEST_MYSQL = \"server=127.0.0.1;port=3306;user=root;password=TU_CLAVE\". " +
                "Las pruebas unitarias no lo necesitan: dotnet test --filter \"FullyQualifiedName~Unit\"",
                exception);
        }

        return new MySqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = string.Empty,
            UserID = "root",
            Password = "root"
        }.ConnectionString;
    }

    /// <summary>
    ///     With migrations, Program.cs already migrated and seeded the admin. Without them (migrations not generated
    ///     yet), the schema is created from the model and the admin is seeded here.
    /// </summary>
    private static async Task EnsureSchemaAsync(NeuroZenApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (context.Database.GetMigrations().Any()) return;

        await context.Database.EnsureCreatedAsync();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TestSeed");
        await IdentitySeeder.SeedAdminAsync(scope.ServiceProvider, configuration, logger);
    }

    private static void Cleanup()
    {
        try
        {
            if (_current is not null)
            {
                using var scope = _current.Factory.Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeleted();
                _current.Factory.Dispose();
            }

            _container?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch
        {
            // Best effort: the database name is unique per run and the container is removed by Testcontainers.
        }
    }
}
