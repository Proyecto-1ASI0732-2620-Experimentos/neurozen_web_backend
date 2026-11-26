using neurozen.API.ResourcesLibrary.Application.Internal;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.Queries;
using neurozen.API.ResourcesLibrary.Domain.Model.ReadModels;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.ResourcesLibrary.Domain.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.ResourcesLibrary.Application.Internal.QueryServices;

/// <summary>Reads apply the plan rules of <see cref="Domain.Model.ValueObjects.ResourcePlan" />.</summary>
public class ResourceLibraryQueryService(IResourceLibraryRepository repository, ResourceAccess access)
    : IResourceLibraryQueryService
{
    /// <summary>Only the resources the caller's plan includes (the others are not listed at all).</summary>
    public async Task<IReadOnlyList<ResourceLibrary>> Handle(GetAllResourceLibrariesQuery query)
    {
        var plan = await access.GetCallerPlanAsync();
        return (await repository.ListOrderedAsync()).Where(r => ResourceAccess.CanSee(plan, r)).ToList();
    }

    /// <summary>
    ///     Same plan filter as the list. The search text is not logged nor stored (it may contain personal or health
    ///     information).
    /// </summary>
    public async Task<IReadOnlyList<ResourceSearchHit>> Handle(SearchResourceLibrariesQuery query)
    {
        var errors = new ValidationErrors();
        var limit = query.Limit ?? SearchResourceLibrariesQuery.DefaultLimit;
        errors.AddIf(limit is < 1 or > SearchResourceLibrariesQuery.MaxLimit, "limit",
            $"limit debe estar entre 1 y {SearchResourceLibrariesQuery.MaxLimit}");

        ResourceType? type = null;
        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            if (ResourceTypeExtensions.TryFromWire(query.Type, out var parsed)) type = parsed;
            else errors.Add("type", "type debe ser Video, Audio, Article o Exercise");
        }

        var tags = (query.Tags ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var terms = ResourceRelevanceScorer.Terms(query.Q, tags);
        errors.AddIf(terms.Count == 0, "q", "Indica un texto de búsqueda (q) o etiquetas (tags)");
        errors.ThrowIfAny();

        var plan = await access.GetCallerPlanAsync();
        return (await repository.ListOrderedAsync())
            .Where(r => ResourceAccess.CanSee(plan, r) && (type is null || r.ResourceType == type))
            .Select(r => new ResourceSearchHit(r, ResourceRelevanceScorer.Score(r, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Resource.Id)
            .Take(limit)
            .ToList();
    }

    /// <summary>404 if it does not exist; 403 "disponible desde el plan X" if the caller's plan does not include it.</summary>
    public async Task<ResourceLibrary> Handle(GetResourceLibraryByIdQuery query)
    {
        var resource = await repository.FindByIdAsync(query.Id)
                       ?? throw new ResourceNotFoundException("El recurso no existe");
        if (!ResourceAccess.CanSee(await access.GetCallerPlanAsync(), resource))
            throw ResourceAccess.PlanRequired(resource);
        return resource;
    }

    public Task<IReadOnlyList<ResourceLibrary>> Handle(GetAllResourceLibrariesForAdminQuery query) =>
        repository.ListOrderedAsync();

    public async Task<ResourceLibrary> Handle(GetResourceLibraryForAdminQuery query) =>
        await repository.FindByIdAsync(query.Id) ?? throw new ResourceNotFoundException("El recurso no existe");
}
