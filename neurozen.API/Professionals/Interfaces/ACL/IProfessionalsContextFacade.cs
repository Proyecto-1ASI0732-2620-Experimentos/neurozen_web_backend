using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.Professionals.Interfaces.ACL;

/// <summary>
///     Minimal professional data for other contexts.
///     IsBookable = VERIFIED and not deleted (visible to patients).
/// </summary>
public record ProfessionalReference(long Id, long UserId, bool IsBookable);

/// <summary>
///     Entry point used by other bounded contexts (IAM sign-up, Appointments) to work with professional profiles.
/// </summary>
public interface IProfessionalsContextFacade
{
    /// <summary>Adds format errors for specialty (enum) and licenseNumber (3-6 digits).</summary>
    void ValidateRegistration(string? specialty, string? licenseNumber, ValidationErrors errors);

    Task<bool> ExistsByLicenseNumberAsync(string licenseNumber);

    /// <summary>
    ///     Adds a PENDING professional profile for the user. Does not save: the caller commits it
    ///     in the same unit of work / transaction as the user.
    /// </summary>
    Task RegisterPendingProfessionalAsync(long userId, string specialty, string licenseNumber);

    /// <summary>
    ///     Called by IAM when the user's full name changes outside the professional profile endpoint: the profile
    ///     goes back to PENDING verification. Does not save: the caller commits in its unit of work.
    /// </summary>
    Task ReturnToPendingVerificationAsync(long userId);

    Task<ProfessionalReference?> FindByIdAsync(long professionalId);

    Task<ProfessionalReference?> FindByUserIdAsync(long userId);

    /// <summary>True when the UTC start is exactly one of the professional's slots (weekday and hour, Lima time).</summary>
    Task<bool> IsWithinAvailabilityAsync(long professionalId, DateTime startUtc);

    /// <summary>
    ///     True when a session of <paramref name="durationMinutes" /> starting at the UTC instant begins a block of the
    ///     professional's schedule (its own grid or the 60-minute grid) and ends before or at the schedule end.
    /// </summary>
    Task<bool> IsWithinAvailabilityAsync(long professionalId, DateTime startUtc, int durationMinutes);
}
