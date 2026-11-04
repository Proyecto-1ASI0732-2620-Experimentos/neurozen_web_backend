using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
///     Used only by the EF Core tools (dotnet ef migrations ...). It builds the model without starting the
///     application, so generating migrations needs neither a running MySQL server nor the JWT secret.
///     "dotnet ef database update" uses ConnectionStrings__DefaultConnection when it is set.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private static readonly MySqlServerVersion DesignTimeServerVersion = new(new Version(8, 0, 36));

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = "server=localhost;port=3306;database=neurozen;user=neurozen;password=design-time-only";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, DesignTimeServerVersion)
            .Options;
        return new AppDbContext(options);
    }
}
