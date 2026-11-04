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

    public async Task<AppointmentParticipant> ResolveAsync(Appointment appointment)
    {
        if (appointment.PatientId == currentUser.Id) return AppointmentParticipant.Patient;

        var professional = await professionalsContextFacade.FindByIdAsync(appointment.ProfessionalId);
        return professional?.UserId == currentUser.Id
            ? AppointmentParticipant.Psychologist
            : AppointmentParticipant.None;
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
