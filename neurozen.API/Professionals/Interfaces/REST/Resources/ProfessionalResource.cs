namespace neurozen.API.Professionals.Interfaces.REST.Resources;

/// <summary>
///     Public directory entry (GET /professionals, GET /professionals/{id}).
///     Never includes licenseNumber or email. availabilitySchedule (Lima time) feeds the booking calendar; null when
///     the professional has not set it. price stays null when it is not defined (never 0).
/// </summary>
public record ProfessionalResource(
    long Id,
    long UserId,
    string Name,
    string Specialty,
    string Bio,
    int? ExperienceYears,
    decimal? Price,
    decimal Rating,
    int Reviews,
    string Availability,
    string Image,
    string VerificationStatus,
    AvailabilityScheduleResource? AvailabilitySchedule = null);
