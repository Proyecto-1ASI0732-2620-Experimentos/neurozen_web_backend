namespace neurozen.API.IAM.Interfaces.REST.Resources;

/// <summary>POST /authentication/sign-in — "username" is the email.</summary>
public record SignInResource(string? Username, string? Password);

/// <summary>
///     POST /authentication/sign-up. "email" is the login ("username" is accepted as a fallback).
///     If "fullName" is missing it is built from firstName + lastName.
///     specialty and licenseNumber are required only for role PSYCHOLOGIST.
///     dni (8 digits) is required for every role. confirmPassword is optional: when sent it must match password
///     and it is never stored. acceptedPrivacyPolicy is optional for backward compatibility: when missing,
///     acceptedTerms (single "Términos y Política de Privacidad" checkbox) is recorded for both documents.
/// </summary>
public record SignUpResource(
    string? Username,
    string? Email,
    string? Password,
    string? FullName,
    string? FirstName,
    string? LastName,
    string? Role,
    bool? AcceptedTerms,
    string? Specialty,
    string? LicenseNumber,
    string? Dni = null,
    string? ConfirmPassword = null,
    bool? AcceptedPrivacyPolicy = null);

public record ForgotPasswordResource(string? Email);

public record ResetPasswordResource(string? Token, string? NewPassword);

/// <summary>Response of sign-in and sign-up.</summary>
public record AuthenticatedUserResource(long Id, string FullName, string Email, string Role, string Token);

public record MessageResource(string Message);
