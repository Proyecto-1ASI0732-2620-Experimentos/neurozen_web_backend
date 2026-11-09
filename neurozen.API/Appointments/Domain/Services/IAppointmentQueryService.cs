using neurozen.API.Appointments.Domain.Model.Queries;
using neurozen.API.Appointments.Domain.Model.ReadModels;
using neurozen.API.Shared.Domain.Model.Pagination;

namespace neurozen.API.Appointments.Domain.Services;

public interface IAppointmentQueryService
{
    Task<PageResult<AppointmentDetails>> Handle(GetAppointmentsQuery query);

    Task<AppointmentDetails> Handle(GetAppointmentByIdQuery query);
}
