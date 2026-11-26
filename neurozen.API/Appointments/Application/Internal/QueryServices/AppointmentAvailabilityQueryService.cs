using neurozen.API.Appointments.Domain.Model.Queries;
using neurozen.API.Appointments.Domain.Services;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Security;

namespace neurozen.API.Appointments.Application.Internal.QueryServices;

/// <summary>
///     Availability by date range for the booking calendar. The slot rules live in the Professionals context
///     (SlotAvailabilityCalculator), reached through its ACL facade, so they are not duplicated here.
/// </summary>
public class AppointmentAvailabilityQueryService(
    IProfessionalsContextFacade professionalsContextFacade,
    IProfessionalAvailabilityFacade professionalAvailabilityFacade,
    ICurrentUser currentUser) : IAppointmentAvailabilityQueryService
{
    public const string OwnAvailabilityOnlyMessage = "Solo puedes consultar tu propia disponibilidad";

    public async Task<AvailabilityCalendar> Handle(GetAppointmentAvailabilityQuery query)
    {
        if (query.ProfessionalId is not { } professionalId)
            throw new ValidationFailedException("professionalId es obligatorio",
                new Dictionary<string, string[]> { ["professionalId"] = ["professionalId es obligatorio"] });

        if (currentUser.IsInRole(AppRoles.Psychologist))
        {
            var own = await professionalsContextFacade.FindByUserIdAsync(currentUser.Id);
            if (own is null || own.Id != professionalId)
                throw new ForbiddenOperationException(OwnAvailabilityOnlyMessage);
        }
        else if (!currentUser.IsInRole(AppRoles.User) && !currentUser.IsInRole(AppRoles.Admin))
        {
            throw new ForbiddenOperationException("No tienes permiso para realizar esta acción");
        }

        return await professionalAvailabilityFacade.GetAvailabilityAsync(professionalId, query.StartDate,
            query.EndDate, query.DurationMinutes);
    }
}
