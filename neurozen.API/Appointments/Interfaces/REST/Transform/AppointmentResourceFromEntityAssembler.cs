using System.Globalization;
using neurozen.API.Appointments.Domain.Model.Commands;
using neurozen.API.Appointments.Domain.Model.ReadModels;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Appointments.Interfaces.REST.Resources;
using neurozen.API.Professionals.Domain.Model.ReadModels;

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

public static class AvailabilityResourceAssembler
{
    private const string DateFormat = "yyyy-MM-dd";

    public static AvailabilityResource ToResource(AvailabilityCalendar calendar) => new(
        calendar.ProfessionalId,
        calendar.TimeZone,
        calendar.SlotMinutes,
        calendar.Days
            .Select(day => new AvailabilityDayResource(
                day.Date.ToString(DateFormat, CultureInfo.InvariantCulture), day.Slots))
            .ToList(),
        calendar.NextAvailableDate?.ToString(DateFormat, CultureInfo.InvariantCulture));
}

public static class CreateAppointmentCommandFromResourceAssembler
{
    public static CreateAppointmentCommand ToCommandFromResource(CreateAppointmentResource resource) =>
        new(resource.PatientId, resource.ProfessionalId, resource.AppointmentDateTime, resource.AppointmentType,
            resource.NotasAdicionales);
}
