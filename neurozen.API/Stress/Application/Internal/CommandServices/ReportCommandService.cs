using neurozen.API.Appointments.Interfaces.ACL;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Commands;
using neurozen.API.Stress.Domain.Model.ReadModels;
using neurozen.API.Stress.Domain.Model.ValueObjects;
using neurozen.API.Stress.Domain.Repositories;
using neurozen.API.Stress.Domain.Services;

namespace neurozen.API.Stress.Application.Internal.CommandServices;

public class ReportCommandService(
    IReportRepository reportRepository,
    IAppointmentsContextFacade appointmentsContextFacade,
    IIamContextFacade iamContextFacade,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IReportCommandService
{
    public const string DuplicateMessage = "Ya compartiste un informe para esta cita";

    public async Task<ReportDetails> Handle(CreateReportCommand command)
    {
        var patientId = StressAccess.ResolveOwnUserId(currentUser, command.PatientId);
        var errors = new ValidationErrors();

        errors.AddIf(command.ProfessionalId is null, "professionalId", "professionalId es obligatorio");
        errors.AddIf(command.AppointmentId is null, "appointmentId", "appointmentId es obligatorio");
        errors.AddIf(command.StressCurrent is null or < 0 or > 100, "stressCurrent",
            "El estrés actual debe estar entre 0 y 100");
        errors.AddIf(command.StressAverage is null or < 0 or > 100, "stressAverage",
            "El estrés promedio debe estar entre 0 y 100");
        errors.AddIf(command.WeeklyChange is < -1000 or > 1000, "weeklyChange",
            "La variación semanal debe estar entre -1000 y 1000");
        errors.AddIf(command.TriggersCount is < 0 or > 100000, "triggersCount",
            "La cantidad de registros no es válida");

        var peakHours = string.IsNullOrWhiteSpace(command.PeakHours) ? null : command.PeakHours.Trim();
        errors.AddIf(peakHours?.Length > Report.PeakHoursMaxLength, "peakHours",
            $"Las horas pico no pueden superar {Report.PeakHoursMaxLength} caracteres");

        var note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
        errors.AddIf(note?.Length > Report.NoteMaxLength, "note",
            $"La nota no puede superar {Report.NoteMaxLength} caracteres");

        var lastScore = command.LastAssessment?.Score;
        errors.AddIf(command.LastAssessment is not null &&
                     (lastScore is null or < 0 or > PerceivedStressScale.MaxScore),
            "lastAssessment.score", $"El puntaje del test debe estar entre 0 y {PerceivedStressScale.MaxScore}");

        errors.ThrowIfAny();

        var appointment = await appointmentsContextFacade.FindAppointmentAsync(command.AppointmentId!.Value);
        if (appointment is null || appointment.PatientId != patientId ||
            appointment.ProfessionalId != command.ProfessionalId!.Value || !appointment.IsActive)
            throw new ValidationFailedException(
                "Solo puedes compartir un informe en una cita tuya, con ese profesional y pendiente o confirmada");

        if (await reportRepository.ExistsByAppointmentIdAsync(appointment.Id))
            throw new ConflictException(DuplicateMessage);

        var topCategory = string.IsNullOrWhiteSpace(command.TopCategory)
            ? (TriggerCategory?)null
            : TriggerCategoryExtensions.FromWireOrOther(command.TopCategory);

        var report = new Report(
            patientId,
            appointment.ProfessionalId,
            appointment.Id,
            Math.Round(command.StressCurrent!.Value, 2),
            Math.Round(command.StressAverage!.Value, 2),
            Math.Round(command.WeeklyChange ?? 0, 2),
            peakHours,
            command.TriggersCount ?? 0,
            topCategory,
            lastScore,
            command.LastAssessment?.CompletedAt,
            note);

        await reportRepository.AddAsync(report);
        await unitOfWork.CompleteAsync();

        var patient = await iamContextFacade.FetchUserSummaryAsync(patientId);
        return new ReportDetails(report, patient?.FullName ?? string.Empty);
    }
}
