namespace neurozen.API.Appointments.Domain.Model.ValueObjects;

public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled
}

public static class AppointmentStatusExtensions
{
    /// <summary>Wire / storage value: PENDING, CONFIRMED, COMPLETED, CANCELLED.</summary>
    public static string ToWire(this AppointmentStatus status) => status.ToString().ToUpperInvariant();

    public static AppointmentStatus FromWire(string value) => Enum.Parse<AppointmentStatus>(value, true);

    public static bool TryFromWire(string? value, out AppointmentStatus status) =>
        Enum.TryParse(value?.Trim(), true, out status) && Enum.IsDefined(status) &&
        !int.TryParse(value, out _);
}
