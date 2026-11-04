namespace neurozen.API.Professionals.Domain.Model.Queries;

public record GetMyProfessionalProfileQuery(long UserId);

/// <summary>Status: PENDING, VERIFIED, REJECTED or null/empty for all.</summary>
public record GetProfessionalsForAdminQuery(string? Status);

/// <summary>Date in Lima time, "YYYY-MM-DD".</summary>
public record GetAvailableSlotsQuery(long ProfessionalId, string? Date);
