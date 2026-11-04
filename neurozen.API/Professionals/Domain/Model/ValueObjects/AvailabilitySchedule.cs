using System.Globalization;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.Professionals.Domain.Model.ValueObjects;

/// <summary>
///     Weekly availability in Lima time: { "days": ["monday","wednesday"], "start": "09:00", "end": "13:00" }.
///     Stored as a JSON column. Sessions are offered in 1-hour slots from "start" while they fit before "end".
/// </summary>
public record AvailabilitySchedule(IReadOnlyList<string> Days, string Start, string End)
{
    public const int SlotMinutes = 60;
    private const string TimeFormat = "HH:mm";

    public static readonly IReadOnlyList<string> WeekDays =
        ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

    private static readonly IReadOnlyDictionary<string, string> SpanishAbbreviations = new Dictionary<string, string>
    {
        ["monday"] = "Lun", ["tuesday"] = "Mar", ["wednesday"] = "Mié", ["thursday"] = "Jue",
        ["friday"] = "Vie", ["saturday"] = "Sáb", ["sunday"] = "Dom"
    };

    /// <summary>Validates raw input and returns a normalized schedule (days lower-cased, unique, in week order), or null.</summary>
    public static AvailabilitySchedule? TryCreate(IEnumerable<string?>? days, string? start, string? end,
        ValidationErrors errors, string field = "availabilitySchedule")
    {
        var normalizedDays = (days ?? Enumerable.Empty<string?>()).Select(day => (day ?? string.Empty).Trim().ToLowerInvariant()).ToList();
        var localErrors = new ValidationErrors();

        localErrors.AddIf(normalizedDays.Count == 0, $"{field}.days", "Selecciona al menos un día de atención");
        foreach (var invalid in normalizedDays.Where(day => !WeekDays.Contains(day)).Distinct())
            localErrors.Add($"{field}.days", $"Día no válido: «{invalid}»");

        var startOk = TryParseTime(start, out var startTime);
        var endOk = TryParseTime(end, out var endTime);
        localErrors.AddIf(!startOk, $"{field}.start", "La hora de inicio debe tener el formato HH:mm");
        localErrors.AddIf(!endOk, $"{field}.end", "La hora de fin debe tener el formato HH:mm");
        localErrors.AddIf(startOk && endOk && endTime <= startTime, $"{field}.end",
            "La hora de fin debe ser posterior a la hora de inicio");

        if (localErrors.HasErrors)
        {
            localErrors.CopyTo(errors);
            return null;
        }

        var orderedDays = WeekDays.Where(normalizedDays.Contains).ToList();
        return new AvailabilitySchedule(orderedDays, startTime.ToString(TimeFormat, CultureInfo.InvariantCulture),
            endTime.ToString(TimeFormat, CultureInfo.InvariantCulture));
    }

    public bool Includes(DayOfWeek dayOfWeek) => Days.Contains(dayOfWeek.ToString().ToLowerInvariant());

    /// <summary>Slot start times (Lima) for a given weekday; empty when the day is not in the schedule.</summary>
    public IReadOnlyList<TimeOnly> SlotsFor(DayOfWeek dayOfWeek)
    {
        if (!Includes(dayOfWeek) || !TryParseTime(Start, out var start) || !TryParseTime(End, out var end))
            return [];

        var slots = new List<TimeOnly>();
        var endMinutes = (int)end.ToTimeSpan().TotalMinutes;
        for (var minutes = (int)start.ToTimeSpan().TotalMinutes; minutes + SlotMinutes <= endMinutes;
             minutes += SlotMinutes)
            slots.Add(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes)));
        return slots;
    }

    /// <summary>Text shown in the directory: "Lun, Mié · 09:00 - 13:00".</summary>
    public string Describe() =>
        $"{string.Join(", ", Days.Select(day => SpanishAbbreviations.GetValueOrDefault(day, day)))} · {Start} - {End}";

    private static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact((value ?? string.Empty).Trim(), TimeFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out time);
}
