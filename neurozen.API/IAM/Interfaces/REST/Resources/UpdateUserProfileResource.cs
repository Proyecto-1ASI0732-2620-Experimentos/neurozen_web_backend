namespace neurozen.API.IAM.Interfaces.REST.Resources;

/// <summary>
///     PUT /users/{id}. Only these fields are bound: role, verificationStatus, password or passwordHash
///     in the body are ignored (no mass assignment). null/missing = keep; "" clears an optional field.
///     "email" is the login; "username" is accepted as a fallback when "email" is missing.
///     dateOfBirth is "YYYY-MM-DD".
/// </summary>
public record UpdateUserProfileResource(
    string? Username,
    string? Email,
    string? FullName,
    string? PhoneNumber,
    string? Address,
    string? AvatarUrl,
    string? DateOfBirth,
    string? AgeRange,
    string? Occupation,
    string? WorkType,
    string? CurrentPassword = null);

/// <summary>PUT /users/{id}/password.</summary>
public record ChangePasswordResource(string? CurrentPassword, string? NewPassword);
