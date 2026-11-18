namespace neurozen.API.IAM.Interfaces.REST.Resources;

/// <summary>POST /authentication/sign-in — "username" is the email.</summary>
public record SignInResource(string? Username, string? Password);

/// <summary>
///     POST /authentication/sign-up. One field per piece of data (no aliases):
///     fullName (first name and last name in one field), dni (8 digits), email (the login), password,
///     confirmPassword (optional; must match and is never stored), role (USER by default or PSYCHOLOGIST),
///     acceptedTerms, acceptedPrivacyPolicy (optional; when missing, acceptedTerms is recorded for both documents),
///     specialty and licenseNumber (required only for PSYCHOLOGIST).
/// </summary>
public record SignUpResource(
    string? FullName,
    string? Dni,
    string? Email,
    string? Password,
    string? ConfirmPassword,
    string? Role,
    bool? AcceptedTerms,
    bool? AcceptedPrivacyPolicy,
    string? Specialty,
    string? LicenseNumber);

public record ForgotPasswordResource(string? Email);

public record ResetPasswordResource(string? Token, string? NewPassword);

/// <summary>Response of sign-in and sign-up.</summary>
public record AuthenticatedUserResource(long Id, string FullName, string Email, string Role, string Token);

public record MessageResource(string Message);
