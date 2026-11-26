namespace neurozen.API.Appointments.Domain.Model.ValueObjects;

/// <summary>Appointment type with its display name and estimated duration.</summary>
public record AppointmentType
{
    private AppointmentType(EAppointmentType type, string displayName, string description, int durationMinutes)
    {
        Type = type;
        DisplayName = displayName;
        Description = description;
        EstimatedDurationMinutes = durationMinutes;
    }

    public EAppointmentType Type { get; init; }
    public string DisplayName { get; init; }
    public string Description { get; init; }
    public int EstimatedDurationMinutes { get; init; }

    public static readonly AppointmentType TerapiaIndividual = new(
        EAppointmentType.TerapiaIndividual, "Terapia individual",
        "Sesión personalizada uno a uno con el profesional", 60);

    public static readonly AppointmentType ConsultaInicial = new(
        EAppointmentType.ConsultaInicial, "Consulta inicial",
        "Primera consulta para evaluación y orientación", 45);

    public static readonly AppointmentType Seguimiento = new(
        EAppointmentType.Seguimiento, "Seguimiento",
        "Sesión de seguimiento y revisión de progreso", 30);

    public static IEnumerable<AppointmentType> GetAll()
    {
        yield return TerapiaIndividual;
        yield return ConsultaInicial;
        yield return Seguimiento;
    }

    public static bool IsValid(int value) => Enum.IsDefined(typeof(EAppointmentType), value);

    /// <summary>Longest duration among the types: how far back an earlier appointment can still overlap.</summary>
    public static int MaxDurationMinutes => GetAll().Max(type => type.EstimatedDurationMinutes);

    public static AppointmentType From(EAppointmentType type) =>
        GetAll().FirstOrDefault(candidate => candidate.Type == type) ?? TerapiaIndividual;

    /// <summary>Duration used to compute the time an appointment occupies (60, 45 or 30 minutes).</summary>
    public static int DurationOf(EAppointmentType type) => From(type).EstimatedDurationMinutes;
}
