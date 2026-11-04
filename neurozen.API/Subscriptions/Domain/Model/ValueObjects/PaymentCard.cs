using System.Globalization;

namespace neurozen.API.Subscriptions.Domain.Model.ValueObjects;

/// <summary>
///     Card checks done on the server (PCI DSS). The full number and the CVV are only validated in memory:
///     they are never stored, logged or returned. Only the last 4 digits and the expiry are kept.
///     In a real deployment the payment is delegated to a gateway (Culqi, Niubiz, Stripe) and only its token is stored.
/// </summary>
public static class PaymentCard
{
    public const int MinDigits = 13;
    public const int MaxDigits = 19;

    /// <summary>Removes spaces and dashes; returns null if anything else than digits remains.</summary>
    public static string? NormalizeNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number)) return null;
        var compact = number.Replace(" ", string.Empty).Replace("-", string.Empty);
        return compact.All(char.IsAsciiDigit) ? compact : null;
    }

    public static bool IsValidNumber(string? normalizedNumber) =>
        normalizedNumber is { Length: >= MinDigits and <= MaxDigits } && PassesLuhn(normalizedNumber);

    public static bool IsValidCvv(string? cvv) =>
        cvv is { Length: 3 or 4 } && cvv.All(char.IsAsciiDigit);

    /// <summary>Accepts "MM/YY" or "MM/YYYY". Returns the normalized "MM/YY".</summary>
    public static bool TryParseExpiry(string? value, out int month, out int year, out string normalized)
    {
        month = 0;
        year = 0;
        normalized = string.Empty;
        var parts = (value ?? string.Empty).Trim().Split('/');
        if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out month) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out year))
            return false;

        if (parts[1].Length == 2) year += 2000;
        else if (parts[1].Length != 4) return false;
        if (month is < 1 or > 12) return false;

        normalized = $"{month:00}/{year % 100:00}";
        return true;
    }

    /// <summary>A card is valid through the last day of its expiry month.</summary>
    public static bool IsExpired(int month, int year, DateTime nowUtc) =>
        new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1) <= nowUtc;

    public static string LastFour(string normalizedNumber) => normalizedNumber[^4..];

    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var doubleIt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';
            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }

            sum += digit;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }
}
