using neurozen.API.Safety.Domain.Model.Entities;
using neurozen.API.Safety.Domain.Model.Queries;

namespace neurozen.API.Safety.Domain.Services;

public interface IEmergencyResourceQueryService
{
    /// <summary>Lines of the country (empty list for a country without verified lines); 400 for an invalid code.</summary>
    IReadOnlyList<EmergencyResource> Handle(GetEmergencyResourcesQuery query);

    /// <summary>Date the team last verified the lines (EmergencyResources:VerifiedAt), or null when not configured.</summary>
    DateOnly? VerifiedAt { get; }
}
