using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.Commands;
using neurozen.API.ResourcesLibrary.Domain.Model.Queries;
using neurozen.API.ResourcesLibrary.Domain.Model.ReadModels;

namespace neurozen.API.ResourcesLibrary.Domain.Services;

public interface IResourceLibraryCommandService
{
    Task<ResourceLibrary> Handle(CreateResourceLibraryCommand command);

    /// <summary>404 when it does not exist; 400 with field errors when a value is invalid.</summary>
    Task<ResourceLibrary> Handle(UpdateResourceLibraryCommand command);
}

public interface IResourceLibraryQueryService
{
    Task<IReadOnlyList<ResourceLibrary>> Handle(GetAllResourceLibrariesQuery query);

    /// <summary>Relevance search over the resources the caller's plan includes; 400 for invalid parameters.</summary>
    Task<IReadOnlyList<ResourceSearchHit>> Handle(SearchResourceLibrariesQuery query);

    /// <summary>404 when it does not exist.</summary>
    Task<ResourceLibrary> Handle(GetResourceLibraryByIdQuery query);

    /// <summary>ADMIN: all resources without the plan filter.</summary>
    Task<IReadOnlyList<ResourceLibrary>> Handle(GetAllResourceLibrariesForAdminQuery query);

    /// <summary>ADMIN: one resource without the plan check; 404 when it does not exist.</summary>
    Task<ResourceLibrary> Handle(GetResourceLibraryForAdminQuery query);
}
