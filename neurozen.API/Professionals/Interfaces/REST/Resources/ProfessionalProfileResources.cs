namespace neurozen.API.Professionals.Interfaces.REST.Resources;

public record AvailabilityScheduleResource(IReadOnlyList<string> Days, string Start, string End);

/// <summary>GET/PUT /professionals/me response (own profile, any verification status).</summary>
public record MyProfessionalProfileResource(
    long Id,
    long UserId,
    string Name,
    string Email,
    string Specialty,
    string LicenseNumber,
    string VerificationStatus,
    string RejectionReason,
    string Bio,
    int? ExperienceYears,
    decimal? Price,
    decimal Rating,
    int Reviews,
    AvailabilityScheduleResource? AvailabilitySchedule,
    string Availability,
    DateTime CreatedAt);

public record AvailabilityScheduleInput(List<string?>? Days, string? Start, string? End);

/// <summary>
///     PUT /professionals/me body. verificationStatus, rating and reviews are not bound (ignored if sent).
/// </summary>
public record UpdateMyProfessionalProfileResource(
    string? FullName,
    string? Specialty,
    string? LicenseNumber,
    string? Bio,
    int? ExperienceYears,
    decimal? Price,
    AvailabilityScheduleInput? AvailabilitySchedule,
    string? Availability);

/// <summary>Admin panel row (GET /admin/professionals and verify/reject responses).</summary>
public record AdminProfessionalResource(
    long Id,
    long UserId,
    string Name,
    string Email,
    string Specialty,
    string LicenseNumber,
    string VerificationStatus,
    string RejectionReason,
    DateTime CreatedAt);

public record RejectProfessionalResource(string? Reason);
