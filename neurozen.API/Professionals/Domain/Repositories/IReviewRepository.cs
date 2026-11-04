using neurozen.API.Professionals.Domain.Model.Entities;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Professionals.Domain.Repositories;

public record ReviewStatistics(decimal AverageRating, int Count);

public interface IReviewRepository : IBaseRepository<Review>
{
    Task<bool> ExistsByAppointmentIdAsync(long appointmentId);

    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<Review>> ListByProfessionalIdAsync(long professionalId);

    /// <summary>Average and count of the persisted reviews of a professional.</summary>
    Task<ReviewStatistics> GetStatisticsAsync(long professionalId);
}
