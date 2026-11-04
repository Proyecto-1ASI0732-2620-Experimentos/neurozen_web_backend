using Microsoft.EntityFrameworkCore;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace neurozen.API.Professionals.Infrastructure.Repositories;

public class ProfessionalRepository(AppDbContext context)
    : BaseRepository<Professional>(context), IProfessionalRepository
{
    public Task<bool> ExistsByLicenseNumberAsync(string licenseNumber) =>
        Context.Set<Professional>().AnyAsync(p => p.LicenseNumber == licenseNumber);

    public Task<bool> ExistsByLicenseNumberForOtherAsync(string licenseNumber, long excludedProfessionalId) =>
        Context.Set<Professional>().AnyAsync(p => p.LicenseNumber == licenseNumber && p.Id != excludedProfessionalId);

    public Task<Professional?> FindByUserIdAsync(long userId) =>
        Context.Set<Professional>().FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task<IEnumerable<Professional>> ListVerifiedAsync() =>
        await Context.Set<Professional>()
            .Where(p => p.VerificationStatus == VerificationStatus.Verified && p.DeletedAt == null)
            .OrderBy(p => p.Id)
            .ToListAsync();

    public async Task<IReadOnlyList<Professional>> ListForAdminAsync(VerificationStatus? status)
    {
        var query = Context.Set<Professional>().Where(p => p.DeletedAt == null);
        if (status is not null) query = query.Where(p => p.VerificationStatus == status.Value);
        return await query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).ToListAsync();
    }
}
