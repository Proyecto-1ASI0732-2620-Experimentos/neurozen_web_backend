using System.Net.Mail;

namespace neurozen.API.IAM.Domain.Model.ValueObjects;

/// <summary>
///     Email rules. Emails are stored trimmed and lower-cased, so lookups are case-insensitive.
/// </summary>
public static class EmailAddress
{
    public const int MaxLength = 254;

    /// <summary>Reserved TLD (RFC 2606) used for anonymized accounts; never accepted from users.</summary>
    public const string ReservedSuffix = ".invalid";

    public static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsValid(string normalizedEmail)
    {
        if (string.IsNullOrEmpty(normalizedEmail) || normalizedEmail.Length > MaxLength) return false;
        if (normalizedEmail.EndsWith(ReservedSuffix, StringComparison.Ordinal)) return false;
        if (!MailAddress.TryCreate(normalizedEmail, out var address)) return false;
        return address.Address == normalizedEmail && normalizedEmail.Contains('.', StringComparison.Ordinal);
    }

    /// <summary>Unique placeholder email for an anonymized (deleted) account.</summary>
    public static string ForDeletedAccount(long userId) => $"deleted-{userId}@deleted.neurozen{ReservedSuffix}";
}
