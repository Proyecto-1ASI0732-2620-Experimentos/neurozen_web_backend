using neurozen.API.Appointments.Domain.Model.Queries;
using neurozen.API.Appointments.Domain.Model.ReadModels;

namespace neurozen.API.Appointments.Domain.Services;

public interface IAppointmentQueryService
{
    Task<IReadOnlyList<AppointmentDetails>> Handle(GetAppointmentsQuery query);

    Task<AppointmentDetails> Handle(GetAppointmentByIdQuery query);
}
