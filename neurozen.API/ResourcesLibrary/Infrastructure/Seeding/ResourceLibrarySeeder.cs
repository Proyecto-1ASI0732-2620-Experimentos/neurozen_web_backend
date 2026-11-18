using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.ResourcesLibrary.Infrastructure.Seeding;

/// <summary>
///     Sample resources for local demos (Seed:Demo:Enabled). Idempotent per title: each sample is added only if no
///     resource with that title exists, so they also appear when the library already holds the YouTube resources
///     loaded by migrations. Existing rows are never modified. Samples are Avanzado (default): the Básico slots
///     (1-2 per type) belong to the real content loaded by migrations.
/// </summary>
public static class ResourceLibrarySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Seed:Demo:Enabled", false)) return;

        var repository = services.GetRequiredService<IResourceLibraryRepository>();
        var samples = new[]
        {
            new ResourceLibrary(
                "Respiración 4-7-8 para la oficina",
                "Técnica breve para bajar la activación antes de una reunión.",
                "Inhala por la nariz contando 4, retén el aire contando 7 y exhala por la boca contando 8. Repite 4 veces.",
                "Equipo NeuroZen", ResourceType.Exercise, null, 5, null, ["respiración", "ansiedad"]),
            new ResourceLibrary(
                "Pausas activas en la jornada",
                "Cómo organizar micro-pausas para reducir la fatiga.",
                "Cada 50 minutos de trabajo toma 5 minutos para estirarte, hidratarte y mirar a lo lejos.",
                "Equipo NeuroZen", ResourceType.Article, null, 4, null, ["hábitos", "productividad"]),
            new ResourceLibrary(
                "Meditación guiada de 10 minutos",
                "Atención a la respiración para cerrar el día.",
                null, "Equipo NeuroZen", ResourceType.Audio,
                "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3", 10, null, ["meditación"])
        };

        var added = false;
        foreach (var sample in samples)
        {
            if (await repository.ExistsByTitleAsync(sample.Title)) continue;
            await repository.AddAsync(sample);
            added = true;
        }

        if (added) await services.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }
}
