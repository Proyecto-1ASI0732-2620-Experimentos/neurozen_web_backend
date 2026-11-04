namespace neurozen.API.IAM.Interfaces.REST.Resources;

/// <summary>GET /users/{id}. Never includes the password hash.</summary>
public record UserResource(
    long Id,
    string Email,
    string FullName,
    string Role,
    string PhoneNumber,
    string Address,
    string AvatarUrl,
    DateOnly? DateOfBirth,
    string AgeRange,
    string Occupation,
    string WorkType,
    DateTime CreatedAt);
