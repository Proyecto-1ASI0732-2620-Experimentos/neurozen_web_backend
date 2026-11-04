using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.ResourcesLibrary.Domain.Repositories;

public interface IResourceLibraryRepository : IBaseRepository<ResourceLibrary>
{
    Task<IReadOnlyList<ResourceLibrary>> ListOrderedAsync();

    Task<bool> AnyAsync();
}
