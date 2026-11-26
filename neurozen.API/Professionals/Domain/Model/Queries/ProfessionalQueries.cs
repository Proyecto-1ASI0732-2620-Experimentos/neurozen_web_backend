namespace neurozen.API.Professionals.Domain.Model.Queries;

public record GetMyProfessionalProfileQuery(long UserId);

/// <summary>Status: PENDING, VERIFIED, REJECTED or null/empty for all.</summary>
public record GetProfessionalsForAdminQuery(string? Status);

/// <summary>
///     Date in Lima time, "YYYY-MM-DD". DurationMinutes: block size (null = 60, valid 15-240).
/// </summary>
public record GetAvailableSlotsQuery(long ProfessionalId, string? Date, int? DurationMinutes = null);

/// <summary>
///     Free slots for every date of [StartDate, EndDate] ("YYYY-MM-DD", Lima time, at most
///     <c>MaxDays</c> days). DurationMinutes: block size (null = 60, valid 15-240).
/// </summary>
public record GetAvailabilityRangeQuery(long ProfessionalId, string? StartDate, string? EndDate,
    int? DurationMinutes = null)
{
    public const int MaxDays = 92;
}
