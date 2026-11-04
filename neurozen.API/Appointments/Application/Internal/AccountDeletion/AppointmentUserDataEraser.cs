using neurozen.API.Appointments.Domain.Repositories;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Application.Accounts;

namespace neurozen.API.Appointments.Application.Internal.AccountDeletion;

/// <summary>
///     Account deletion: future (PENDING/CONFIRMED) appointments are cancelled — as a patient and, for a
///     psychologist, on their professional profile. Past appointments stay for the other participant's
///     history; the patient's notes are removed (personal data) and the name is shown as «Usuario eliminado».
///     Runs before the professional eraser (which anonymizes the profile).
/// </summary>
public class AppointmentUserDataEraser(
    IAppointmentRepository appointmentRepository,
    IProfessionalsContextFacade professionalsContextFacade) : IUserDataEraser
{
    public int Order => 400;

    public async Task EraseAsync(long userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var appointment in await appointmentRepository.ListByPatientIdAsync(userId))
        {
            if (appointment.OccupiesSlot && appointment.AppointmentDateTime > now) appointment.Cancel(now);
            appointment.ClearNotes(now);
        }

        var professional = await professionalsContextFacade.FindByUserIdAsync(userId);
        if (professional is null) return;

        foreach (var appointment in await appointmentRepository.ListByProfessionalIdAsync(professional.Id))
            if (appointment.OccupiesSlot && appointment.AppointmentDateTime > now)
                appointment.Cancel(now);
    }
}
