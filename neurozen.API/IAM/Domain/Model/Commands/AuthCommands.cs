namespace neurozen.API.IAM.Domain.Model.Commands;

public record SignInCommand(string? Email, string? Password);

/// <summary>
///     Sign-up data. <paramref name="ConfirmPassword" /> is checked only when the client sends it and is never
///     persisted. <paramref name="AcceptedPrivacyPolicy" /> = null means the client still uses the single
///     "Términos y Política de Privacidad" checkbox, so <paramref name="AcceptedTerms" /> is used for both records.
/// </summary>
public record SignUpCommand(
    string? Email,
    string? Password,
    string? FullName,
    string? Role,
    bool AcceptedTerms,
    string? Specialty,
    string? LicenseNumber,
    string? Dni = null,
    string? ConfirmPassword = null,
    bool? AcceptedPrivacyPolicy = null);

public record ForgotPasswordCommand(string? Email);

public record ResetPasswordCommand(string? Token, string? NewPassword);
