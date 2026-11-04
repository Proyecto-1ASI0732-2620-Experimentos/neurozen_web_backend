using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Professionals.Domain.Repositories;

public interface IProfessionalRepository : IBaseRepository<Professional>
{
    Task<bool> ExistsByLicenseNumberAsync(string licenseNumber);

    Task<bool> ExistsByLicenseNumberForOtherAsync(string licenseNumber, long excludedProfessionalId);

    Task<Professional?> FindByUserIdAsync(long userId);

    /// <summary>Professionals visible to patients (VERIFIED and not deleted).</summary>
    Task<IEnumerable<Professional>> ListVerifiedAsync();

    /// <summary>Not deleted, optionally filtered by status, oldest first (arrival order).</summary>
    Task<IReadOnlyList<Professional>> ListForAdminAsync(VerificationStatus? status);
}
