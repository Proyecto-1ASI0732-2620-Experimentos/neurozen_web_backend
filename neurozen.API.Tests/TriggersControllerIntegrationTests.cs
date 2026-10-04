using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

//Prueba Integral de petición HTTP (Falta arreglar)
public class TriggersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TriggersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTrigger_StressLevelGreaterThan10_ReturnsBadRequest()
    {
        // Arrange (Preparar el JSON inválido)
        var invalidTrigger = new 
        { 
            PatientId = 1, 
            CategoryId = 1, 
            StressLevel = 15, // Inválido
            TriggerDateTime = DateTime.UtcNow, 
            Description = "Ataque de pánico" 
        };
        
        var content = new StringContent(JsonSerializer.Serialize(invalidTrigger), Encoding.UTF8, "application/json");

        // Act (Hacer la petición real al endpoint)
        var response = await _client.PostAsync("/api/v1/Triggers", content);

        // Assert (Verificar el código HTTP)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}