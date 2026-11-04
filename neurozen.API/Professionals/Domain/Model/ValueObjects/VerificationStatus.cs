namespace neurozen.API.Professionals.Domain.Model.ValueObjects;

public enum VerificationStatus
{
    Pending,
    Verified,
    Rejected
}

public static class VerificationStatusExtensions
{
    /// <summary>Wire / storage value: PENDING, VERIFIED, REJECTED.</summary>
    public static string ToWire(this VerificationStatus status) => status.ToString().ToUpperInvariant();

    public static VerificationStatus FromWire(string value) => Enum.Parse<VerificationStatus>(value, true);

    public static bool TryFromWire(string? value, out VerificationStatus status) =>
        Enum.TryParse(value?.Trim(), true, out status) && Enum.IsDefined(status);
}
