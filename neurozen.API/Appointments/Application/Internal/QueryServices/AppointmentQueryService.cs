using neurozen.API.Appointments.Domain.Model.Queries;
using neurozen.API.Appointments.Domain.Model.ReadModels;
using neurozen.API.Appointments.Domain.Repositories;
using neurozen.API.Appointments.Domain.Services;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Security;

namespace neurozen.API.Appointments.Application.Internal.QueryServices;

public class AppointmentQueryService(
    IAppointmentRepository appointmentRepository,
    IProfessionalsContextFacade professionalsContextFacade,
    AppointmentAccess access,
    ICurrentUser currentUser) : IAppointmentQueryService
{
    public async Task<IReadOnlyList<AppointmentDetails>> Handle(GetAppointmentsQuery query)
    {
        if (query.PatientId is null == query.ProfessionalId is null)
            throw new ValidationFailedException("Indica patientId o professionalId (solo uno)");

        if (query.PatientId is { } patientId)
        {
            if (!currentUser.IsInRole(AppRoles.User) || patientId != currentUser.Id)
                throw new ForbiddenOperationException("Solo puedes ver tus propias citas");

            return await access.ToDetailsAsync(await appointmentRepository.ListByPatientIdAsync(patientId), false);
        }

        var professionalId = query.ProfessionalId!.Value;
        if (!currentUser.IsInRole(AppRoles.Psychologist))
            throw new ForbiddenOperationException("Solo puedes ver tus propias citas");

        var professional = await professionalsContextFacade.FindByUserIdAsync(currentUser.Id);
        if (professional is null || professional.Id != professionalId)
            throw new ForbiddenOperationException("Solo puedes ver tus propias citas");
        if (!professional.IsBookable)
            throw new ForbiddenOperationException("Tu cuenta profesional aún no está verificada");

        return await access.ToDetailsAsync(await appointmentRepository.ListByProfessionalIdAsync(professionalId), true);
    }

    public async Task<AppointmentDetails> Handle(GetAppointmentByIdQuery query)
    {
        var appointment = await appointmentRepository.FindByIdAsync(query.Id)
                          ?? throw new ResourceNotFoundException(AppointmentAccess.NotFoundMessage);

        var participant = await access.ResolveAsync(appointment);
        if (participant == AppointmentParticipant.None)
            throw new ResourceNotFoundException(AppointmentAccess.NotFoundMessage);

        return await access.ToDetailsAsync(appointment, participant == AppointmentParticipant.Psychologist);
    }
}
