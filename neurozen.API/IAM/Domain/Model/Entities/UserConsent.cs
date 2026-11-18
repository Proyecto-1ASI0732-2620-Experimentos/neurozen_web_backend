namespace neurozen.API.IAM.Domain.Model.Entities;

/// <summary>
///     Kind of consent recorded at sign-up. Each purpose is a separate record (no generic consent that mixes
///     different purposes). New purposes (e.g. a specific consent for health data) are added here when the
///     legal text and the frontend checkbox exist; the backend never assumes them.
/// </summary>
public enum ConsentType
{
    /// <summary>Terms and conditions of use.</summary>
    Terms,

    /// <summary>Privacy policy / processing of personal data (Ley 29733 and D.S. 016-2024-JUS).</summary>
    PrivacyPolicy
}

public static class ConsentTypeExtensions
{
    /// <summary>Wire / storage value: TERMS, PRIVACY_POLICY.</summary>
    public static string ToWire(this ConsentType type) => type switch
    {
        ConsentType.Terms => "TERMS",
        ConsentType.PrivacyPolicy => "PRIVACY_POLICY",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static ConsentType FromWire(string value) => value switch
    {
        "TERMS" => ConsentType.Terms,
        "PRIVACY_POLICY" => ConsentType.PrivacyPolicy,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown consent type")
    };
}

/// <summary>
///     Evidence of an explicit consent: who accepted, what (type + document version) and when (UTC).
///     Rows are append-only. They hold no personal data besides the user id, so they are kept when the account
///     is anonymized as proof of the lawful basis of past processing.
/// </summary>
public class UserConsent
{
    public const int VersionMaxLength = 20;

    protected UserConsent()
    {
        Version = string.Empty;
    }

    public UserConsent(long userId, ConsentType type, string version, DateTime acceptedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Trim().Length > VersionMaxLength)
            throw new ArgumentException("The document version is required (max 20 characters)", nameof(version));

        UserId = userId;
        Type = type;
        Version = version.Trim();
        AcceptedAt = acceptedAtUtc;
    }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public ConsentType Type { get; private set; }
    public string Version { get; private set; }
    public DateTime AcceptedAt { get; private set; }
}
