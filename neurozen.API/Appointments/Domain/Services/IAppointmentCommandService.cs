using neurozen.API.Appointments.Domain.Model.Commands;
using neurozen.API.Appointments.Domain.Model.ReadModels;

namespace neurozen.API.Appointments.Domain.Services;

public interface IAppointmentCommandService
{
    Task<AppointmentDetails> Handle(CreateAppointmentCommand command);

    Task<AppointmentDetails> Handle(UpdateAppointmentStatusCommand command);
}
