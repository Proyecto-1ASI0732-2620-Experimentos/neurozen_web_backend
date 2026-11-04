namespace neurozen.API.Shared.Domain.Model.Time;

/// <summary>
///     Lima local time. Peru has used UTC-5 with no daylight saving time since 1994, so a fixed
///     offset is used (independent of the OS time zone database, identical on Windows and Linux).
///     Schedules and slots are expressed in Lima time; everything stored or sent as a date-time is UTC.
/// </summary>
public static class LimaTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-5);

    public static DateOnly Today(DateTime? nowUtc = null) =>
        DateOnly.FromDateTime((nowUtc ?? DateTime.UtcNow) + Offset);

    /// <summary>Lima wall-clock date and time → UTC instant.</summary>
    public static DateTime ToUtc(DateOnly date, TimeOnly time) =>
        DateTime.SpecifyKind(date.ToDateTime(time) - Offset, DateTimeKind.Utc);

    /// <summary>UTC instant → Lima wall-clock date and time.</summary>
    public static (DateOnly Date, TimeOnly Time) FromUtc(DateTime utc)
    {
        var lima = DateTime.SpecifyKind(utc, DateTimeKind.Utc) + Offset;
        return (DateOnly.FromDateTime(lima), TimeOnly.FromDateTime(lima));
    }
}
