using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

public class TriggersControllerIntegrationTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TriggersControllerIntegrationTests(IntegrationTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task CreateTrigger_StressLevelGreaterThan10_ReturnsBadRequest()
    {
        // Arrange creamos un usuario test
        var prefijo = Guid.NewGuid().ToString().Substring(0, 8);
        var signUp = new
        {
            Username = $"test_{prefijo}",
            Password = "Password123.",
            Email = $"test_{prefijo}@gmail.com"
        };

        //Registramos al usuario
        var signUpResponse = await _client.PostAsync("/api/v1/Authentication/sign-up",
            new StringContent(JsonSerializer.Serialize(signUp), Encoding.UTF8, "application/json"));
        var signUpResponseStr = await signUpResponse.Content.ReadAsStringAsync();
        Assert.True(signUpResponse.IsSuccessStatusCode,
            $"Sign-up failed with {(int)signUpResponse.StatusCode}: {signUpResponseStr}");

        //Iniciamos sesión
        var signIn = new { Username = signUp.Username, Password = signUp.Password };
        var signInResponse = await _client.PostAsync("/api/v1/Authentication/sign-in",
            new StringContent(JsonSerializer.Serialize(signIn), Encoding.UTF8, "application/json"));

        var signInResponseStr = await signInResponse.Content.ReadAsStringAsync();
        Assert.True(signInResponse.IsSuccessStatusCode,
            $"Sign-in failed with {(int)signInResponse.StatusCode}: {signInResponseStr}");

        //Extraemos el token
        var tokenDoc = JsonDocument.Parse(signInResponseStr);
        var token = tokenDoc.RootElement.GetProperty("token").GetString();

        //Inyectamos el token en el cliente HTTP
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        //Arrange:  preparamos la info inválida para el Trigger
        var invalidTrigger = new
        {
            PatientId = 1,
            CategoryId = 1,
            StressLevel = 15, // Aquí esta el error
            TriggerDateTime = DateTime.UtcNow,
            Description = "Ataque de pánico"
        };
        var content = new StringContent(JsonSerializer.Serialize(invalidTrigger), Encoding.UTF8, "application/json");

        //Hacemos la petición
        var response = await _client.PostAsync("/api/v1/Triggers", content);

        //Assert: Verificamos que la API bloquea el nivel de estrés en 10
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var currentConfiguration = configuration.Build();
            var connectionString = currentConfiguration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("DefaultConnection is required for integration tests.");

            var testConnectionString = connectionString
                .Replace("database=neurozenDev", "database=neurozenIntegrationTests",
                    StringComparison.OrdinalIgnoreCase);

            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = testConnectionString
            });
        });
    }
}