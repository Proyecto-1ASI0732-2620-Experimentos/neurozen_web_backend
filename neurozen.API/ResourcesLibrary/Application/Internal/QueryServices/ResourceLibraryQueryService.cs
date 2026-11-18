using neurozen.API.ResourcesLibrary.Application.Internal;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.Queries;
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
