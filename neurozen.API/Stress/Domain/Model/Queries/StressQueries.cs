namespace neurozen.API.Stress.Domain.Model.Queries;

public record GetTriggersQuery(long? UserId);

public record GetAssessmentsQuery(long? UserId);

/// <summary>Exactly one of PatientId / ProfessionalId must be given.</summary>
public record GetReportsQuery(long? PatientId, long? ProfessionalId);
