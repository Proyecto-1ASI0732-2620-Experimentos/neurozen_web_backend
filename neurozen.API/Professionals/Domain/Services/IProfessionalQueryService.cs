using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Model.ReadModels;

namespace neurozen.API.Professionals.Domain.Services;

public interface IProfessionalQueryService
{
    /// <summary>Directory: VERIFIED and not deleted only.</summary>
    Task<IReadOnlyList<ProfessionalDetails>> Handle(GetAllProfessionalsQuery query);

    /// <summary>404 when it does not exist or is not visible to patients.</summary>
    Task<ProfessionalDetails> Handle(GetProfessionalByIdQuery query);

    /// <summary>Own profile in any verification status. 404 when the account has no professional profile.</summary>
    Task<ProfessionalDetails> Handle(GetMyProfessionalProfileQuery query);

    /// <summary>Admin panel, oldest first.</summary>
    Task<IReadOnlyList<ProfessionalDetails>> Handle(GetProfessionalsForAdminQuery query);

    /// <summary>Free slots ("HH:mm", Lima time) for a date; 60-minute blocks unless DurationMinutes is given.</summary>
    Task<IReadOnlyList<string>> Handle(GetAvailableSlotsQuery query);

    /// <summary>Free slots per date over a range (max 92 days) plus the next available date.</summary>
    Task<AvailabilityCalendar> Handle(GetAvailabilityRangeQuery query);
}
