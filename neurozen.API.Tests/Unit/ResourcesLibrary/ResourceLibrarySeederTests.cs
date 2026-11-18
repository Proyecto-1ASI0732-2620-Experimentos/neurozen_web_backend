using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.ResourcesLibrary.Infrastructure.Seeding;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Tests.Unit.ResourcesLibrary;

public class ResourceLibrarySeederTests
{
    private static (IServiceProvider Services, Mock<IResourceLibraryRepository> Repository, Mock<IUnitOfWork> UnitOfWork)
        Build(params string[] existingTitles)
    {
        var repository = new Mock<IResourceLibraryRepository>();
        repository.Setup(r => r.ExistsByTitleAsync(It.IsAny<string>()))
            .ReturnsAsync((string title) => existingTitles.Contains(title));
        var unitOfWork = new Mock<IUnitOfWork>();
        var services = new ServiceCollection()
            .AddSingleton(repository.Object)
            .AddSingleton(unitOfWork.Object)
            .BuildServiceProvider();
        return (services, repository, unitOfWork);
    }

    private static IConfiguration Demo(bool enabled) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:Demo:Enabled"] = enabled.ToString() })
        .Build();

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: antes el seeder de demo no hacía nada si la tabla tenía cualquier fila, así
    ///   que tras cargar los 12 recursos de YouTube los ejemplos de demo desaparecían en local.
    /// - ¿Qué hace esta prueba?: con un ejemplo ya existente, solo se agregan los otros dos y se guarda una vez.
    /// </summary>
    [Fact]
    public async Task Seed_AddsOnlyMissingSamples_EvenIfLibraryIsNotEmpty()
    {
        var (services, repository, unitOfWork) = Build("Pausas activas en la jornada");

        await ResourceLibrarySeeder.SeedAsync(services, Demo(true));

        repository.Verify(r => r.AddAsync(It.IsAny<ResourceLibrary>()), Times.Exactly(2));
        repository.Verify(r => r.AddAsync(It.Is<ResourceLibrary>(x => x.Title == "Pausas activas en la jornada")),
            Times.Never);
        unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task Seed_WhenAllSamplesExist_DoesNothing()
    {
        var (services, repository, unitOfWork) = Build("Respiración 4-7-8 para la oficina",
            "Pausas activas en la jornada", "Meditación guiada de 10 minutos");

        await ResourceLibrarySeeder.SeedAsync(services, Demo(true));

        repository.Verify(r => r.AddAsync(It.IsAny<ResourceLibrary>()), Times.Never);
        unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task Seed_WhenDemoDisabled_DoesNothing()
    {
        var (services, repository, _) = Build();

        await ResourceLibrarySeeder.SeedAsync(services, Demo(false));

        repository.Verify(r => r.ExistsByTitleAsync(It.IsAny<string>()), Times.Never);
        repository.Verify(r => r.AddAsync(It.IsAny<ResourceLibrary>()), Times.Never);
    }
}
