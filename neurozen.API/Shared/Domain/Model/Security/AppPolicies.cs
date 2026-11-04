namespace neurozen.API.Shared.Domain.Model.Security;

/// <summary>Authorization policy names shared by every bounded context.</summary>
public static class AppPolicies
{
    /// <summary>
    ///     Any authenticated user, except PSYCHOLOGIST accounts that are not VERIFIED
    ///     (they may only use /professionals/me and their own account).
    /// </summary>
    public const string VerifiedIfPsychologist = "VerifiedIfPsychologist";

    /// <summary>Role PSYCHOLOGIST with a VERIFIED, active professional profile.</summary>
    public const string VerifiedPsychologist = "VerifiedPsychologist";
}
