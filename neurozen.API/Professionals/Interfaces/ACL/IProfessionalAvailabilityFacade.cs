using neurozen.API.Professionals.Domain.Model.ReadModels;

namespace neurozen.API.Professionals.Interfaces.ACL;

/// <summary>Availability calendar of a professional, for other contexts (Appointments).</summary>
public interface IProfessionalAvailabilityFacade
{
    /// <summary>
    ///     Free slots per date (same rules as GET /professionals/{id}/available-slots). 400 for invalid dates, a range
    ///     over 92 days or an invalid duration; 404 when the professional does not exist or is not verified.
    /// </summary>
    Task<AvailabilityCalendar> GetAvailabilityAsync(long professionalId, string? startDate, string? endDate,
        int? durationMinutes);
}
