using neurozen.API.Appointments.Domain.Model.Commands;
using neurozen.API.Appointments.Domain.Model.ReadModels;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Appointments.Interfaces.REST.Resources;

namespace neurozen.API.Appointments.Interfaces.REST.Transform;

public static class AppointmentResourceFromEntityAssembler
{
    public static AppointmentResource ToResourceFromEntity(AppointmentDetails details)
    {
        var a = details.Appointment;
        return new AppointmentResource(
            a.Id,
            a.PatientId,
            details.PatientName,
            details.PatientEmail,
            a.ProfessionalId,
            a.AppointmentDateTime,
            (int)a.AppointmentType,
            a.Status.ToWire(),
            a.NotasAdicionales ?? string.Empty,
            a.Reviewed);
    }

    public static AppointmentTypeResource ToResource(AppointmentType type) => new(
        (int)type.Type,
        type.Type.ToString(),
        type.DisplayName,
        type.Description,
        type.EstimatedDurationMinutes);
}

public static class CreateAppointmentCommandFromResourceAssembler
{
    public static CreateAppointmentCommand ToCommandFromResource(CreateAppointmentResource resource) =>
        new(resource.PatientId, resource.ProfessionalId, resource.AppointmentDateTime, resource.AppointmentType,
            resource.NotasAdicionales);
}
