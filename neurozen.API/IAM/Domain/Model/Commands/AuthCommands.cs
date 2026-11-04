namespace neurozen.API.IAM.Domain.Model.Commands;

public record SignInCommand(string? Email, string? Password);

public record SignUpCommand(
    string? Email,
    string? Password,
    string? FullName,
    string? Role,
    bool AcceptedTerms,
    string? Specialty,
    string? LicenseNumber);

public record ForgotPasswordCommand(string? Email);

public record ResetPasswordCommand(string? Token, string? NewPassword);
