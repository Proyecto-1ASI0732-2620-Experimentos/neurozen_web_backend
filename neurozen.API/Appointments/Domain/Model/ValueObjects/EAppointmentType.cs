namespace neurozen.API.Appointments.Domain.Model.ValueObjects;

/// <summary>Appointment types. The numeric value travels in JSON ("appointmentType": 1).</summary>
public enum EAppointmentType
{
    /// <summary>Terapia individual (60 min).</summary>
    TerapiaIndividual = 1,

    /// <summary>Consulta inicial (45 min).</summary>
    ConsultaInicial = 2,

    /// <summary>Seguimiento (30 min).</summary>
    Seguimiento = 3
}
