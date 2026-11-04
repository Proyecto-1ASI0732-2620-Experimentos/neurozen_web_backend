using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.Commands;
using neurozen.API.ResourcesLibrary.Domain.Model.Queries;

namespace neurozen.API.ResourcesLibrary.Domain.Services;

public interface IResourceLibraryCommandService
{
    Task<ResourceLibrary> Handle(CreateResourceLibraryCommand command);
}

public interface IResourceLibraryQueryService
{
    Task<IReadOnlyList<ResourceLibrary>> Handle(GetAllResourceLibrariesQuery query);

    /// <summary>404 when it does not exist.</summary>
    Task<ResourceLibrary> Handle(GetResourceLibraryByIdQuery query);
}
