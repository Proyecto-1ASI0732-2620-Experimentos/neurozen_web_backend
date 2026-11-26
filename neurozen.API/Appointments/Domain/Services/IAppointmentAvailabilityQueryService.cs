using neurozen.API.Appointments.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Model.ReadModels;

namespace neurozen.API.Appointments.Domain.Services;

public interface IAppointmentAvailabilityQueryService
{
    /// <summary>
    ///     USER and ADMIN: any verified professional. PSYCHOLOGIST: only their own profile (403 otherwise).
    ///     400 for invalid dates or a range over 92 days; 404 when the professional does not exist or is not verified.
    /// </summary>
    Task<AvailabilityCalendar> Handle(GetAppointmentAvailabilityQuery query);
}
