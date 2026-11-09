namespace neurozen.API.Stress.Domain.Model.Queries;

public record GetTriggersQuery(long? UserId, int? Page = null, int? PageSize = null);

public record GetAssessmentsQuery(long? UserId, int? Page = null, int? PageSize = null);

/// <summary>Exactly one of PatientId / ProfessionalId must be given.</summary>
public record GetReportsQuery(long? PatientId, long? ProfessionalId, int? Page = null, int? PageSize = null);
