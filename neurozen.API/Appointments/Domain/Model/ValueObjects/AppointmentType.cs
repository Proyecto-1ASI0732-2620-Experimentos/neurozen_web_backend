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
}
