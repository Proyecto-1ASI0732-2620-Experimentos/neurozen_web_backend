namespace neurozen.API.Professionals.Domain.Model.Commands;

/// <summary>PUT /professionals/me — the frontend always sends the whole profile.</summary>
public record UpdateMyProfessionalProfileCommand(
    long UserId,
    string? FullName,
    string? Specialty,
    string? LicenseNumber,
    string? Bio,
    int? ExperienceYears,
    decimal? Price,
    bool HasAvailabilitySchedule,
    IReadOnlyList<string?>? AvailabilityDays,
    string? AvailabilityStart,
    string? AvailabilityEnd,
    string? Availability);

public record VerifyProfessionalCommand(long ProfessionalId, long AdminUserId);

public record RejectProfessionalCommand(long ProfessionalId, long AdminUserId, string? Reason);
