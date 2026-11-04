using Microsoft.EntityFrameworkCore;
using neurozen.API.Professionals.Domain.Model.Entities;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace neurozen.API.Professionals.Infrastructure.Repositories;

public class ReviewRepository(AppDbContext context) : BaseRepository<Review>(context), IReviewRepository
{
    public Task<bool> ExistsByAppointmentIdAsync(long appointmentId) =>
        Context.Set<Review>().AnyAsync(r => r.AppointmentId == appointmentId);

    public async Task<IReadOnlyList<Review>> ListByProfessionalIdAsync(long professionalId) =>
        await Context.Set<Review>()
            .AsNoTracking()
            .Where(r => r.ProfessionalId == professionalId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .ToListAsync();

    public async Task<ReviewStatistics> GetStatisticsAsync(long professionalId)
    {
        var ratings = Context.Set<Review>().Where(r => r.ProfessionalId == professionalId);
        var count = await ratings.CountAsync();
        if (count == 0) return new ReviewStatistics(0, 0);

        var average = await ratings.AverageAsync(r => (decimal)r.Rating);
        return new ReviewStatistics(average, count);
    }
}
