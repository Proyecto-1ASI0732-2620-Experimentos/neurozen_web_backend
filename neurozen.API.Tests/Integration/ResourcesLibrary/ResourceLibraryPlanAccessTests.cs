using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.Tests.Integration.Support;

namespace neurozen.API.Tests.Integration.ResourcesLibrary;

/// <summary>
///     Acceso a la biblioteca según el plan: Básico (o sin suscripción) = 1-2 recursos de cada tipo; Avanzado = toda la
///     biblioteca actual; Profesional = todo, incluido lo marcado como Profesional. El backend no lista ni entrega el
///     contentUrl de un recurso fuera del plan.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class ResourceLibraryPlanAccessTests(IntegrationFixture fixture)
{
    private static readonly string[] BasicYouTubeTitles =
    [
        "Técnicas de Respiración Profunda", "Ejercicios de Estiramiento para Oficina",
        "Meditación Guiada de 5 Minutos", "Música Relajante para Dormir"
    ];

    private static readonly string[] AllYouTubeTitles = ResourceLibraryYouTubeLinksTests.ExpectedLinks
        .Select(row => (string)row[0]).ToArray();

    private TestEnvironment Env => fixture.Environment;

    private static async Task<List<JsonElement>> ListAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/resource-libraries");
        await TestApi.EnsureStatusAsync(response, HttpStatusCode.OK);
        return (await response.ReadJsonAsync()).EnumerateArray().ToList();
    }

    private static string[] YouTubeTitlesIn(List<JsonElement> items) => items
        .Select(i => i.GetProperty("title").GetString()!)
        .Where(AllYouTubeTitles.Contains).Order(StringComparer.Ordinal).ToArray();

    private Task<long> IdOfAsync(string title) => Env.QueryDatabaseAsync(db =>
        db.Set<ResourceLibrary>().Where(r => r.Title == title).Select(r => r.Id).SingleAsync());

    private async Task<long> CreateProfessionalOnlyResourceAsync()
    {
        var admin = await Env.Api.AsAdmin();
        var created = await admin.Client.PostAsJsonAsync("/api/v1/resource-libraries", new
        {
            title = $"Recurso Profesional {Guid.NewGuid():N}", resourceType = "Video",
            contentUrl = "https://www.youtube.com/watch?v=WkzMBxohi1M", duration = 5, requiredPlan = 3
        });
        await TestApi.EnsureStatusAsync(created, HttpStatusCode.Created);
        return (await created.ReadJsonAsync()).GetProperty("id").GetInt64();
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el plan Básico (y quien aún no tiene suscripción) solo ve 1-2 recursos de
    ///   cada tipo; de los 12 de YouTube, 2 videos y 2 audios.
    /// - ¿Qué hace esta prueba?: lista la biblioteca sin plan y con plan Básico y compara los títulos visibles; además,
    ///   ningún tipo aparece más de 2 veces.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task BasicOrNoPlan_SeesOnlyOneToTwoOfEachType(int? planId)
    {
        var user = planId is null ? await Env.Api.AsUser() : await Env.Api.AsUserWithPlan(planId.Value);

        var items = await ListAsync(user.Client);

        Assert.Equal(BasicYouTubeTitles.Order(StringComparer.Ordinal), YouTubeTitlesIn(items));
        Assert.All(items.GroupBy(i => i.GetProperty("resourceType").GetString()),
            group => Assert.InRange(group.Count(), 1, 2));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el backend no debe entregar el enlace de un recurso fuera del plan aunque
    ///   se pida directamente por id (no basta con ocultarlo en el listado).
    /// - ¿Qué hace esta prueba?: un usuario Básico pide por id un recurso Avanzado: 403 con el plan necesario y sin
    ///   contentUrl en el cuerpo; el mismo usuario sí abre un recurso Básico.
    /// </summary>
    [Fact]
    public async Task Basic_RequestingAdvancedResourceById_Gets403WithoutLink()
    {
        var user = await Env.Api.AsUserWithPlan(1);
        var advancedId = await IdOfAsync("Yoga para Reducir la Ansiedad");
        var basicId = await IdOfAsync("Técnicas de Respiración Profunda");

        var locked = await user.Client.GetAsync($"/api/v1/resource-libraries/{advancedId}");
        var alias = await user.Client.GetAsync($"/api/v1/resources/{advancedId}");
        var allowed = await user.Client.GetAsync($"/api/v1/resource-libraries/{basicId}");

        Assert.Equal(HttpStatusCode.Forbidden, locked.StatusCode);
        Assert.Equal("Este recurso está disponible desde el plan Avanzado", await locked.MessageAsync());
        var raw = await locked.Content.ReadAsStringAsync();
        Assert.DoesNotContain("contentUrl", raw);
        Assert.DoesNotContain("tS52C0Pxf_g", raw);
        Assert.Equal(HttpStatusCode.Forbidden, alias.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el plan Avanzado ve toda la biblioteca actual, pero no lo marcado como
    ///   Profesional; el plan Profesional lo ve todo.
    /// - ¿Qué hace esta prueba?: crea un recurso solo Profesional y compara lo que ven Avanzado y Profesional (listado y
    ///   detalle).
    /// </summary>
    [Fact]
    public async Task Advanced_SeesWholeLibrary_ButNotProfessional_AndProfessionalSeesEverything()
    {
        var professionalId = await CreateProfessionalOnlyResourceAsync();
        var advanced = await Env.Api.AsUserWithPlan(2);
        var professional = await Env.Api.AsUserWithPlan(3);

        var advancedItems = await ListAsync(advanced.Client);
        var professionalItems = await ListAsync(professional.Client);
        var advancedDetail = await advanced.Client.GetAsync($"/api/v1/resource-libraries/{professionalId}");
        var professionalDetail = await professional.Client.GetAsync($"/api/v1/resource-libraries/{professionalId}");

        Assert.Equal(AllYouTubeTitles.Order(StringComparer.Ordinal), YouTubeTitlesIn(advancedItems));
        Assert.DoesNotContain(advancedItems, i => i.GetProperty("id").GetInt64() == professionalId);
        Assert.Equal(HttpStatusCode.Forbidden, advancedDetail.StatusCode);
        Assert.Equal("Este recurso está disponible desde el plan Profesional", await advancedDetail.MessageAsync());

        Assert.Equal(AllYouTubeTitles.Order(StringComparer.Ordinal), YouTubeTitlesIn(professionalItems));
        Assert.Contains(professionalItems, i => i.GetProperty("id").GetInt64() == professionalId);
        Assert.Equal(HttpStatusCode.OK, professionalDetail.StatusCode);
        var total = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().CountAsync());
        Assert.Equal(total, professionalItems.Count);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: al subir de plan el acceso cambia de inmediato (no hay caché por usuario).
    /// - ¿Qué hace esta prueba?: un usuario Básico pasa a Avanzado y ya ve los 12 recursos.
    /// </summary>
    [Fact]
    public async Task UpgradingPlan_UnlocksResourcesImmediately()
    {
        var user = await Env.Api.AsUserWithPlan(1);
        Assert.Equal(4, YouTubeTitlesIn(await ListAsync(user.Client)).Length);

        await TestApi.SubscribeAsync(user, 2);

        Assert.Equal(12, YouTubeTitlesIn(await ListAsync(user.Client)).Length);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: psicólogos verificados y administradores no tienen suscripción, pero usan
    ///   la biblioteca con sus pacientes o la administran: lo ven todo.
    /// - ¿Qué hace esta prueba?: compara su listado con el total de la tabla.
    /// </summary>
    [Fact]
    public async Task VerifiedPsychologistAndAdmin_SeeEverything()
    {
        await CreateProfessionalOnlyResourceAsync();
        var psychologist = await Env.Api.AsPsychologist();
        var admin = await Env.Api.AsAdmin();
        var total = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().CountAsync());

        Assert.Equal(total, (await ListAsync(psychologist.Client)).Count);
        Assert.Equal(total, (await ListAsync(admin.Client)).Count);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el admin decide en qué plan va cada recurso; el valor debe validarse.
    /// - ¿Qué hace esta prueba?: un plan inválido da 400; pasar un recurso Avanzado a Básico lo hace visible para un
    ///   usuario Básico (y se restaura al final).
    /// </summary>
    [Fact]
    public async Task AdminCanChangeRequiredPlan_WithValidation()
    {
        var admin = await Env.Api.AsAdmin();
        var basicUser = await Env.Api.AsUserWithPlan(1);
        const string title = "Podcast: Manejo de Crisis de Ansiedad";
        var id = await IdOfAsync(title);

        var invalid = await admin.Client.PatchAsJsonAsync($"/api/v1/resource-libraries/{id}", new { requiredPlan = 7 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.True((await invalid.ReadJsonAsync()).GetProperty("errors").TryGetProperty("requiredPlan", out _));

        try
        {
            var toBasic = await admin.Client.PatchAsJsonAsync($"/api/v1/resource-libraries/{id}",
                new { requiredPlan = 1 });
            Assert.Equal(HttpStatusCode.OK, toBasic.StatusCode);
            Assert.Contains(title, YouTubeTitlesIn(await ListAsync(basicUser.Client)));
        }
        finally
        {
            await Env.WithDatabaseAsync(db => db.Set<ResourceLibrary>().Where(r => r.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.RequiredPlan, ResourcePlan.Advanced)));
        }
    }
}
