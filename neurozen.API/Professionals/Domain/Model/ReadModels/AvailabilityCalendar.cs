namespace neurozen.API.Professionals.Domain.Model.ReadModels;

/// <summary>Free slots ("HH:mm", Lima time) of one date.</summary>
public record AvailabilityDay(DateOnly Date, IReadOnlyList<string> Slots);

/// <summary>
///     Availability of a professional over a date range. NextAvailableDate is the first date with at least one free
///     slot from the start of the range (or today, if later); it may be after the range end, or null when there is
///     none within the search horizon.
/// </summary>
public record AvailabilityCalendar(
    long ProfessionalId,
    string TimeZone,
    int SlotMinutes,
    IReadOnlyList<AvailabilityDay> Days,
    DateOnly? NextAvailableDate);
