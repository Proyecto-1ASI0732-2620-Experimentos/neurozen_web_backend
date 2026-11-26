using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Domain.Services;

namespace neurozen.API.Professionals.Interfaces.ACL.Services;

public class ProfessionalAvailabilityFacade(IProfessionalQueryService professionalQueryService)
    : IProfessionalAvailabilityFacade
{
    public Task<AvailabilityCalendar> GetAvailabilityAsync(long professionalId, string? startDate, string? endDate,
        int? durationMinutes) =>
        professionalQueryService.Handle(
            new GetAvailabilityRangeQuery(professionalId, startDate, endDate, durationMinutes));
}
