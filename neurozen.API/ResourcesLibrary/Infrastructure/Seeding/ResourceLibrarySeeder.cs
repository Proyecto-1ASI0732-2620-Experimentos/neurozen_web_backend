using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.ResourcesLibrary.Infrastructure.Seeding;

/// <summary>Sample resources for local demos (Seed:Demo:Enabled), only when the library is empty.</summary>
public static class ResourceLibrarySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Seed:Demo:Enabled", false)) return;

        var repository = services.GetRequiredService<IResourceLibraryRepository>();
        if (await repository.AnyAsync()) return;

        await repository.AddAsync(new ResourceLibrary(
            "Respiración 4-7-8 para la oficina",
            "Técnica breve para bajar la activación antes de una reunión.",
            "Inhala por la nariz contando 4, retén el aire contando 7 y exhala por la boca contando 8. Repite 4 veces.",
            "Equipo NeuroZen", ResourceType.Exercise, null, 5, null, ["respiración", "ansiedad"]));
        await repository.AddAsync(new ResourceLibrary(
            "Pausas activas en la jornada",
            "Cómo organizar micro-pausas para reducir la fatiga.",
            "Cada 50 minutos de trabajo toma 5 minutos para estirarte, hidratarte y mirar a lo lejos.",
            "Equipo NeuroZen", ResourceType.Article, null, 4, null, ["hábitos", "productividad"]));
        await repository.AddAsync(new ResourceLibrary(
            "Meditación guiada de 10 minutos",
            "Atención a la respiración para cerrar el día.",
            null, "Equipo NeuroZen", ResourceType.Audio,
            "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3", 10, null, ["meditación"]));

        await services.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }
}
