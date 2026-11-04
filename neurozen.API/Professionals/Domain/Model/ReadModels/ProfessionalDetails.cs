using neurozen.API.Professionals.Domain.Model.Aggregates;

namespace neurozen.API.Professionals.Domain.Model.ReadModels;

/// <summary>A professional together with the owner's name and email (from IAM).</summary>
public record ProfessionalDetails(Professional Professional, string Name, string Email);
