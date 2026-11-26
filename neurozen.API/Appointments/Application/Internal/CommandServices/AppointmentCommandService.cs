using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.Commands;
using neurozen.API.Appointments.Domain.Model.ReadModels;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Appointments.Domain.Repositories;
using neurozen.API.Appointments.Domain.Services;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Appointments.Application.Internal.CommandServices;

public class AppointmentCommandService(
    IAppointmentRepository appointmentRepository,
    IProfessionalsContextFacade professionalsContextFacade,
    AppointmentAccess access,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IAppointmentCommandService
{
    public const string SlotTakenMessage = "El horario ya no está disponible";

    public const string OutsideAvailabilityMessage =
        "El horario elegido no está dentro de la disponibilidad del profesional";

    public async Task<AppointmentDetails> Handle(CreateAppointmentCommand command)
    {
        var errors = new ValidationErrors();
        errors.AddIf(command.PatientId is null, "patientId", "patientId es obligatorio");
        errors.AddIf(command.ProfessionalId is null, "professionalId", "professionalId es obligatorio");
        errors.AddIf(command.AppointmentDateTime is null, "appointmentDateTime", "La fecha y hora son obligatorias");
        errors.AddIf(command.AppointmentType is null || !AppointmentType.IsValid(command.AppointmentType.Value),
            "appointmentType", "El tipo de cita debe ser 1, 2 o 3");
        var notes = string.IsNullOrWhiteSpace(command.NotasAdicionales) ? null : command.NotasAdicionales.Trim();
        errors.AddIf(notes?.Length > Appointment.NotesMaxLength, "notasAdicionales",
            $"Las notas no pueden superar {Appointment.NotesMaxLength} caracteres");
        errors.ThrowIfAny();

        if (command.PatientId!.Value != currentUser.Id)
            throw new ForbiddenOperationException("Solo puedes reservar citas para tu propia cuenta");

        var professionalId = command.ProfessionalId!.Value;
        var professional = await professionalsContextFacade.FindByIdAsync(professionalId);
        if (professional is not { IsBookable: true })
            throw new ResourceNotFoundException("El profesional no existe");

        var startUtc = command.AppointmentDateTime!.Value;
        if (startUtc <= DateTime.UtcNow)
            throw new ValidationFailedException("La cita debe programarse en una fecha futura");

        var type = (EAppointmentType)command.AppointmentType!.Value;
        var durationMinutes = AppointmentType.DurationOf(type);

        // 409 (not 400): the frontend asks for another hour without losing the form data.
        if (!await IsWithinAvailabilityAsync(professionalId, startUtc, durationMinutes))
            throw new ConflictException(OutsideAvailabilityMessage);

        Appointment? appointment = null;
        // The professional's row is locked for the whole check-and-insert, so two simultaneous bookings that
        // overlap (even with different start times) are serialized; the unique index
        // (professional_id, active_slot) remains the last line of defence for identical start times.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await appointmentRepository.LockProfessionalScheduleAsync(professionalId);

            if (await appointmentRepository.IsSlotTakenAsync(professionalId, startUtc) ||
                await appointmentRepository.HasOverlappingActiveAppointmentAsync(professionalId, startUtc,
                    durationMinutes))
                throw new ConflictException(SlotTakenMessage);

            appointment = new Appointment(currentUser.Id, professionalId, startUtc, type, notes);
            await appointmentRepository.AddAsync(appointment);
            await unitOfWork.CompleteAsync();
        });

        return await access.ToDetailsAsync(appointment!, false);
    }

    public async Task<AppointmentDetails> Handle(UpdateAppointmentStatusCommand command)
    {
        var appointment = await appointmentRepository.FindByIdAsync(command.AppointmentId)
                          ?? throw new ResourceNotFoundException(AppointmentAccess.NotFoundMessage);

        var participant = await access.ResolveAsync(appointment);
        if (participant == AppointmentParticipant.None)
            throw new ResourceNotFoundException(AppointmentAccess.NotFoundMessage);

        if (!AppointmentStatusExtensions.TryFromWire(command.Status, out var target) ||
            target == AppointmentStatus.Pending)
            throw new ValidationFailedException("El estado debe ser CONFIRMED, CANCELLED o COMPLETED",
                new Dictionary<string, string[]>
                    { ["status"] = ["El estado debe ser CONFIRMED, CANCELLED o COMPLETED"] });

        var now = DateTime.UtcNow;
        switch (target)
        {
            case AppointmentStatus.Confirmed:
                EnsurePsychologist(participant, "Solo el psicólogo de la cita puede confirmarla");
                await access.EnsureVerifiedProfessionalAsync(appointment);
                appointment.Confirm(now);
                break;
            case AppointmentStatus.Completed:
                EnsurePsychologist(participant, "Solo el psicólogo de la cita puede marcarla como atendida");
                await access.EnsureVerifiedProfessionalAsync(appointment);
                appointment.Complete(now);
                break;
            case AppointmentStatus.Cancelled:
                appointment.Cancel(now);
                break;
        }

        await unitOfWork.CompleteAsync();
        // A non-verified psychologist may still cancel (to free the patient), but does not receive the patient e-mail.
        var includePatientEmail = participant == AppointmentParticipant.Psychologist &&
                                  (target != AppointmentStatus.Cancelled ||
                                   await professionalsContextFacade.FindByIdAsync(appointment.ProfessionalId)
                                       is { IsBookable: true });

        return await access.ToDetailsAsync(appointment, includePatientEmail);
    }

    /// <summary>
    ///     60-minute sessions keep the original check; shorter types may also start on their own grid
    ///     (e.g. 30-minute blocks), as offered by available-slots?durationMinutes=.
    /// </summary>
    private async Task<bool> IsWithinAvailabilityAsync(long professionalId, DateTime startUtc, int durationMinutes) =>
        await professionalsContextFacade.IsWithinAvailabilityAsync(professionalId, startUtc) ||
        (durationMinutes != Professionals.Domain.Services.SlotAvailabilityCalculator.DefaultDurationMinutes &&
         await professionalsContextFacade.IsWithinAvailabilityAsync(professionalId, startUtc, durationMinutes));

    private static void EnsurePsychologist(AppointmentParticipant participant, string message)
    {
        if (participant != AppointmentParticipant.Psychologist) throw new ForbiddenOperationException(message);
    }
}
