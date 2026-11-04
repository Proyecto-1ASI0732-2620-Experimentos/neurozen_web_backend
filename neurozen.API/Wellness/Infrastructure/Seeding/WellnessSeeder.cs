using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Infrastructure.Seeding;

/// <summary>Seeds the initial meditation (previously inserted with raw SQL at startup).</summary>
public static class WellnessSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Set<Meditation>().AnyAsync()) return;

        context.Add(new Meditation(
            "Calma Mental y Mindfulness",
            "Sesión guiada para reducir la rumiación cognitiva.",
            15,
            "https://images.unsplash.com/photo-1518199266791",
            "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3"));
        await context.SaveChangesAsync();
    }
}
