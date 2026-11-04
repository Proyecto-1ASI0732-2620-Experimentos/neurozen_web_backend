namespace neurozen.API.IAM.Application.Internal.Configuration;

/// <summary>Bound from the "PasswordReset" configuration section.</summary>
public class PasswordResetSettings
{
    public const int MaxTokenLifetimeMinutes = 60;

    /// <summary>Base URL of the frontend; the link is {FrontendBaseUrl}/reset-password?token=...</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>Capped at 60 minutes by the specification.</summary>
    public int TokenLifetimeMinutes { get; set; } = MaxTokenLifetimeMinutes;

    public int MaxRequestsPerEmail { get; set; } = 3;

    public int EmailWindowMinutes { get; set; } = 15;

    public TimeSpan EffectiveTokenLifetime =>
        TimeSpan.FromMinutes(Math.Clamp(TokenLifetimeMinutes, 5, MaxTokenLifetimeMinutes));
}
