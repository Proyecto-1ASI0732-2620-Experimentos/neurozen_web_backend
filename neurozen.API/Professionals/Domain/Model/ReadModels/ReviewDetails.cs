using neurozen.API.Professionals.Domain.Model.Entities;

namespace neurozen.API.Professionals.Domain.Model.ReadModels;

/// <summary>Review with the abbreviated patient name ("Ana T.") or «Usuario eliminado».</summary>
public record ReviewDetails(Review Review, string UserName);
