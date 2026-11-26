using System.Text.Json;
using Moq;
using neurozen.API.ResourcesLibrary.Application.Internal;
using neurozen.API.ResourcesLibrary.Application.Internal.QueryServices;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.Queries;
using neurozen.API.ResourcesLibrary.Domain.Model.ReadModels;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.ResourcesLibrary.Domain.Services;
using neurozen.API.ResourcesLibrary.Interfaces.REST.Transform;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Subscriptions.Interfaces.ACL;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.ResourcesLibrary;

/// <summary>
///     Búsqueda para ZenBot: respeta el plan como GET /resource-libraries, ordena por relevancia (título y etiquetas
///     pesan más que la descripción) y no distingue tildes ni mayúsculas.
/// </summary>
public class ResourceSearchTests
{
    private readonly Mock<IResourceLibraryRepository> _repository = new();
    private readonly Mock<ISubscriptionsContextFacade> _subscriptions = new();

    private static ResourceLibrary Resource(long id, string title, string description, string[] tags,
        ResourceType type = ResourceType.Video, ResourcePlan plan = ResourcePlan.Basic) =>
        new ResourceLibrary(title, description, null, null, type, $"https://example.org/{id}", 10, null, tags, plan)
            .WithId(id);

    public ResourceSearchTests()
    {
        _repository.Setup(r => r.ListOrderedAsync()).ReturnsAsync(new List<ResourceLibrary>
        {
            Resource(1, "Música para dormir", "Sonidos suaves; también ayuda con la ansiedad", ["sueño"],
                ResourceType.Audio),
            Resource(2, "Respiración para la ansiedad", "Ejercicio de cuatro tiempos", ["respiracion"],
                ResourceType.Exercise),
            Resource(3, "Pausa activa", "Estiramientos en la oficina", ["ANSIEDAD", "oficina"]),
            Resource(4, "Ansiedad avanzada", "Programa completo", ["ansiedad"], plan: ResourcePlan.Advanced),
            Resource(5, "Meditación guiada", "Atención plena", ["mindfulness"])
        });
    }

    private ResourceLibraryQueryService Service(long userId, string role, int? planId)
    {
        _subscriptions.Setup(s => s.GetActivePlanIdAsync(userId)).ReturnsAsync(planId);
        return new ResourceLibraryQueryService(_repository.Object,
            new ResourceAccess(new FakeCurrentUser(userId, role), _subscriptions.Object));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: ZenBot no puede recomendar recursos fuera del plan del usuario.
    /// - ¿Qué hace esta prueba?: busca «ansiedad» con plan Básico (no aparece el recurso Avanzado) y con plan Avanzado
    ///   (sí aparece).
    /// </summary>
    [Fact]
    public async Task Search_ShouldRespectThePlan()
    {
        // Act
        var basic = await Service(7, "USER", 1).Handle(new SearchResourceLibrariesQuery("ansiedad", null, null, 10));
        var advanced = await Service(8, "USER", 2).Handle(new SearchResourceLibrariesQuery("ansiedad", null, null, 10));

        // Assert
        Assert.DoesNotContain(basic, hit => hit.Resource.Id == 4);
        Assert.Contains(advanced, hit => hit.Resource.Id == 4);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: título y etiquetas pesan más que la descripción, sin importar tildes ni
    ///   mayúsculas.
    /// - ¿Qué hace esta prueba?: busca «ÁNSIEDAD» y comprueba el orden: título (2), etiqueta (3) y al final la
    ///   descripción (1); el recurso sin coincidencias no aparece.
    /// </summary>
    [Fact]
    public async Task Search_ShouldOrderByRelevance_IgnoringAccentsAndCase()
    {
        // Act
        var hits = await Service(7, "USER", 1).Handle(new SearchResourceLibrariesQuery("ÁNSIEDAD", null, null, null));

        // Assert
        Assert.Equal([2, 3, 1], hits.Select(hit => hit.Resource.Id));
        Assert.True(hits[0].Score > hits[1].Score);
        Assert.True(hits[1].Score > hits[2].Score);
        Assert.All(hits, hit => Assert.InRange(hit.Score, 0.01, 1));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: tags y type filtran/ponderan, y limit corta la lista.
    /// - ¿Qué hace esta prueba?: tags=respiración con type=Exercise devuelve solo el ejercicio; limit=1 devuelve uno.
    /// </summary>
    [Fact]
    public async Task Search_TagsTypeAndLimit()
    {
        // Arrange
        var service = Service(1, "ADMIN", null);

        // Act
        var byTag = await service.Handle(new SearchResourceLibrariesQuery(null, "respiración, ", "exercise", 5));
        var limited = await service.Handle(new SearchResourceLibrariesQuery("ansiedad", null, null, 1));

        // Assert
        Assert.Equal(2, Assert.Single(byTag).Resource.Id);
        Assert.Single(limited);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: parámetros inválidos → 400 con el campo afectado.
    /// - ¿Qué hace esta prueba?: sin q ni tags, limit fuera de 1-10 y type desconocido.
    /// </summary>
    [Fact]
    public async Task Search_InvalidParameters_ShouldFail()
    {
        // Arrange
        var service = Service(7, "USER", 1);

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            service.Handle(new SearchResourceLibrariesQuery(" a ", null, "Podcast", 11)));

        // Assert
        Assert.True(exception.Errors!.ContainsKey("q"));
        Assert.True(exception.Errors!.ContainsKey("limit"));
        Assert.True(exception.Errors!.ContainsKey("type"));
        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            service.Handle(new SearchResourceLibrariesQuery("ansiedad", null, null, 0)));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la normalización quita tildes y mayúsculas; un texto vacío no puntúa.
    /// - ¿Qué hace esta prueba?: normaliza y puntúa casos límite.
    /// </summary>
    [Fact]
    public void Scorer_Normalization_And_EdgeCases()
    {
        Assert.Equal("ninos y sueno", ResourceRelevanceScorer.Normalize("  NIÑOS   y  Sueño "));
        Assert.Equal(string.Empty, ResourceRelevanceScorer.Normalize(null));
        Assert.Equal(["estres", "laboral"], ResourceRelevanceScorer.Terms("¿Estrés laboral? a", null));
        Assert.Equal(0, ResourceRelevanceScorer.Score(Resource(9, "x", "y", []), []));
        Assert.Equal(1, ResourceRelevanceScorer.Score(Resource(9, "Estrés", "", []), ["estres"]));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la búsqueda entrega durationMinutes y score; el listado y el detalle
    ///   conservan su forma congelada con duration (minutos), sin alias.
    /// - ¿Qué hace esta prueba?: serializa los recursos y comprueba los campos.
    /// </summary>
    [Fact]
    public void SearchExposesDurationMinutes_ListKeepsFrozenShape()
    {
        // Arrange
        var resource = Resource(2, "Respiración", "d", ["respiracion"]);
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var item = JsonSerializer.SerializeToElement(ResourceLibraryAssembler.ToResource(resource), web);
        var detail = JsonSerializer.SerializeToElement(ResourceLibraryAssembler.ToDetailResource(resource), web);
        var hit = JsonSerializer.SerializeToElement(
            ResourceLibraryAssembler.ToSearchItem(new ResourceSearchHit(resource, 0.82)), web);

        // Assert
        Assert.Equal(10, item.GetProperty("duration").GetInt32());
        Assert.False(item.TryGetProperty("durationMinutes", out _));
        Assert.Equal(10, detail.GetProperty("duration").GetInt32());
        Assert.Equal(10, hit.GetProperty("durationMinutes").GetInt32());
        Assert.Equal(0.82, hit.GetProperty("score").GetDouble());
        Assert.Equal("Video", hit.GetProperty("resourceType").GetString());
    }
}
