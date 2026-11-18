using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.ReadModels;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Application.Security;

namespace neurozen.API.Appointments.Application.Internal;

/// <summary>How the caller relates to an appointment.</summary>
public enum AppointmentParticipant
{
    None,
    Patient,
    Psychologist
}

/// <summary>Participant resolution and read-model building shared by the query and command services.</summary>
public class AppointmentAccess(
    ICurrentUser currentUser,
    IProfessionalsContextFacade professionalsContextFacade,
    IIamContextFacade iamContextFacade)
{
    public const string NotFoundMessage = "La cita no existe";
    public const string NotVerifiedMessage = "Tu cuenta profesional aún no está verificada";

    public async Task<AppointmentParticipant> ResolveAsync(Appointment appointment)
    {
        if (appointment.PatientId == currentUser.Id) return AppointmentParticipant.Patient;

        var professional = await professionalsContextFacade.FindByIdAsync(appointment.ProfessionalId);
        return professional?.UserId == currentUser.Id
            ? AppointmentParticipant.Psychologist
            : AppointmentParticipant.None;
    }

    /// <summary>
    ///     A psychologist whose profile is not VERIFIED (e.g. back to PENDING after changing the license number or
    ///     the name) cannot read patient data nor perform professional actions on the appointment (403).
    ///     Same rule as GET /appointments?professionalId=.
    /// </summary>
    public async Task EnsureVerifiedProfessionalAsync(Appointment appointment)
    {
        var professional = await professionalsContextFacade.FindByIdAsync(appointment.ProfessionalId);
        if (professional is not { IsBookable: true })
            throw new Shared.Domain.Model.Exceptions.ForbiddenOperationException(NotVerifiedMessage);
    }

    /// <summary>Adds patientName (and patientEmail only when <paramref name="includePatientEmail" />).</summary>
    public async Task<IReadOnlyList<AppointmentDetails>> ToDetailsAsync(IReadOnlyList<Appointment> appointments,
        bool includePatientEmail)
    {
        var patients = await iamContextFacade.FetchUserSummariesAsync(appointments.Select(a => a.PatientId));
        return appointments
            .Select(a =>
            {
                patients.TryGetValue(a.PatientId, out var patient);
                return new AppointmentDetails(
                    a,
                    patient?.FullName ?? string.Empty,
                    includePatientEmail && patient is { IsDeleted: false } ? patient.Email : null);
            })
            .ToList();
    }

    public async Task<AppointmentDetails> ToDetailsAsync(Appointment appointment, bool includePatientEmail) =>
        (await ToDetailsAsync([appointment], includePatientEmail))[0];
}
