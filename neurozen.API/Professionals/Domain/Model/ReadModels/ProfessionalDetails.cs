using neurozen.API.Professionals.Domain.Model.Aggregates;

namespace neurozen.API.Professionals.Domain.Model.ReadModels;

/// <summary>A professional together with the owner's name, email and DNI (from IAM). Dni only reaches admin views.</summary>
public record ProfessionalDetails(Professional Professional, string Name, string Email, string? Dni = null);
