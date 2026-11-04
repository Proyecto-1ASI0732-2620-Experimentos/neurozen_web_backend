using Microsoft.EntityFrameworkCore;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace neurozen.API.ResourcesLibrary.Infrastructure.Repositories;

public class ResourceLibraryRepository(AppDbContext context)
    : BaseRepository<ResourceLibrary>(context), IResourceLibraryRepository
{
    public async Task<IReadOnlyList<ResourceLibrary>> ListOrderedAsync() =>
        await Context.Set<ResourceLibrary>().AsNoTracking().OrderBy(r => r.Id).ToListAsync();

    public Task<bool> AnyAsync() => Context.Set<ResourceLibrary>().AnyAsync();
}
