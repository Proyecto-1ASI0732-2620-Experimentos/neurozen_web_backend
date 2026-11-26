namespace neurozen.API.Safety.Domain.Model.Queries;

/// <summary>Country: ISO 3166-1 alpha-2 code (default "PE").</summary>
public record GetEmergencyResourcesQuery(string? Country);
