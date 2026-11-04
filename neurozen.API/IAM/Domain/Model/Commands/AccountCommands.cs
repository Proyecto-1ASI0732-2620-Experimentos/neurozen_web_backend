namespace neurozen.API.IAM.Domain.Model.Commands;

/// <summary>
///     PUT /users/{id}. For every field: null = keep the current value.
///     For optional fields an empty string clears the value. Email and full name cannot be cleared.
/// </summary>
public record UpdateUserProfileCommand(
    long UserId,
    string? Email,
    string? FullName,
    string? PhoneNumber,
    string? Address,
    string? AvatarUrl,
    string? DateOfBirth,
    string? AgeRange,
    string? Occupation,
    string? WorkType);

public record ChangePasswordCommand(long UserId, string? CurrentPassword, string? NewPassword);

public record DeleteAccountCommand(long UserId);
