using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.Queries;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.ResourcesLibrary.Domain.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.ResourcesLibrary.Application.Internal.QueryServices;

public class ResourceLibraryQueryService(IResourceLibraryRepository repository) : IResourceLibraryQueryService
{
    public Task<IReadOnlyList<ResourceLibrary>> Handle(GetAllResourceLibrariesQuery query) =>
        repository.ListOrderedAsync();

    public async Task<ResourceLibrary> Handle(GetResourceLibraryByIdQuery query) =>
        await repository.FindByIdAsync(query.Id) ?? throw new ResourceNotFoundException("El recurso no existe");
}
