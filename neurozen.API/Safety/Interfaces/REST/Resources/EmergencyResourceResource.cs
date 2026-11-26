namespace neurozen.API.Safety.Interfaces.REST.Resources;

/// <summary>verifiedAt: "YYYY-MM-DD" of the last verification by the team, or null when it has not been recorded.</summary>
public record EmergencyResourceResource(
    string Name,
    string Phone,
    string Description,
    string Country,
    string? VerifiedAt);
