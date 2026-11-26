using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Shared.Domain.Model.Time;

namespace neurozen.API.Professionals.Domain.Services;

/// <summary>A UTC time range [StartUtc, EndUtc) already taken by a PENDING or CONFIRMED appointment.</summary>
public readonly record struct BookedInterval(DateTime StartUtc, DateTime EndUtc)
{
    public static BookedInterval FromStart(DateTime startUtc, int durationMinutes) =>
        new(startUtc, startUtc.AddMinutes(durationMinutes));

    public bool Overlaps(DateTime startUtc, DateTime endUtc) => StartUtc < endUtc && startUtc < EndUtc;
}

/// <summary>
///     Single source of truth for free slots (GET /professionals/{id}/available-slots and
///     GET /appointments/availability). Pure domain logic, no I/O:
///     <list type="bullet">
///         <item>only weekdays included in <see cref="AvailabilitySchedule.Days" />;</item>
///         <item>blocks of <c>durationMinutes</c> from <c>start</c> while they end before or at <c>end</c> (Lima time);</item>
///         <item>a block overlapping any booked interval (PENDING/CONFIRMED appointment, with its own duration) is excluded;</item>
///         <item>blocks that already started (now or in the past) are excluded.</item>
///     </list>
/// </summary>
public static class SlotAvailabilityCalculator
{
    public const int DefaultDurationMinutes = AvailabilitySchedule.SlotMinutes;
    public const int MinDurationMinutes = 15;
    public const int MaxDurationMinutes = 240;

    /// <summary>IANA name of the time zone in which schedules and slots are expressed.</summary>
    public const string TimeZoneId = "America/Lima";

    public static bool IsValidDuration(int durationMinutes) =>
        durationMinutes is >= MinDurationMinutes and <= MaxDurationMinutes;

    /// <summary>Free block start times (Lima, ascending) for one date.</summary>
    public static IReadOnlyList<TimeOnly> FreeSlots(AvailabilitySchedule? schedule, DateOnly date, int durationMinutes,
        IEnumerable<BookedInterval> booked, DateTime nowUtc)
    {
        if (schedule is null || date < LimaTime.Today(nowUtc)) return [];

        var candidates = schedule.SlotsFor(date.DayOfWeek, durationMinutes);
        if (candidates.Count == 0) return [];

        var bookedList = booked as IReadOnlyCollection<BookedInterval> ?? booked.ToList();
        return candidates
            .Where(slot =>
            {
                var startUtc = LimaTime.ToUtc(date, slot);
                var endUtc = startUtc.AddMinutes(durationMinutes);
                return startUtc > nowUtc && !bookedList.Any(interval => interval.Overlaps(startUtc, endUtc));
            })
            .ToList();
    }

    /// <summary>
    ///     True when a UTC start (whole minute) begins a block of the schedule for the given duration, or a block of the
    ///     default 60-minute grid (kept so that clients that only ask for 60-minute slots can still book shorter types).
    ///     The whole session must end before or at the schedule end.
    /// </summary>
    public static bool IsScheduledStart(AvailabilitySchedule? schedule, DateTime startUtc, int durationMinutes)
    {
        if (schedule is null || startUtc.Second != 0 || startUtc.Millisecond != 0) return false;

        var (date, time) = LimaTime.FromUtc(startUtc);
        if (schedule.SlotsFor(date.DayOfWeek, durationMinutes).Contains(time)) return true;

        return durationMinutes <= DefaultDurationMinutes &&
               schedule.SlotsFor(date.DayOfWeek, DefaultDurationMinutes).Contains(time);
    }
}
