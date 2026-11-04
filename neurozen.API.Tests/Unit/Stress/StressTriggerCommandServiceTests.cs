using Moq;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Stress.Application.Internal.CommandServices;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Commands;
using neurozen.API.Stress.Domain.Model.ValueObjects;
using neurozen.API.Stress.Domain.Repositories;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Stress;

public class StressTriggerCommandServiceTests
{
    private readonly Mock<IStressTriggerRepository> _triggers = new();
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork =
        TestEntities.PassThroughUnitOfWork();

    private StressTriggerCommandService CreateService() =>
        new(_triggers.Object, new FakeCurrentUser(7, "USER"), _unitOfWork.Object);

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la intensidad alimenta los gráficos y los informes al psicólogo; si se
    ///   confiara en la del cliente, se podría registrar un nivel 9 como «low». Se calcula siempre en el servidor
    ///   (1-3 low, 4-6 medium, 7-10 high).
    /// - ¿Qué hace esta prueba?: registra situaciones en los límites de cada tramo y comprueba la intensidad guardada.
    ///   El comando ni siquiera tiene un campo de intensidad: lo que envíe el cliente se ignora.
    /// </summary>
    [Theory]
    [InlineData(1, "low")]
    [InlineData(3, "low")]
    [InlineData(4, "medium")]
    [InlineData(6, "medium")]
    [InlineData(7, "high")]
    [InlineData(10, "high")]
    public async Task Trigger_IntensityShouldBeCalculatedOnServer(int stressLevel, string expectedIntensity)
    {
        // Arrange
        StressTrigger? saved = null;
        _triggers.Setup(t => t.AddAsync(It.IsAny<StressTrigger>()))
            .Callback<StressTrigger>(t => saved = t).Returns(Task.CompletedTask);
        var command = new CreateTriggerCommand(7, "Tres entregas el mismo día", "workOverload", stressLevel,
            DateTime.UtcNow.AddHours(-1));

        // Act
        await CreateService().Handle(command);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(expectedIntensity, saved!.Intensity.ToWire());
        Assert.Equal(TriggerCategory.WorkOverload, saved.Category);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un registro de estrés no puede tener fecha futura (distorsiona las
    ///   tendencias) ni una descripción de menos de 5 caracteres.
    /// - ¿Qué hace esta prueba?: envía una fecha a una hora en el futuro y una descripción de 3 letras; ambos dan 400
    ///   en el campo correspondiente y no se guarda nada.
    /// </summary>
    [Theory]
    [InlineData("futura", "triggeredAt")]
    [InlineData("corta", "description")]
    public async Task Trigger_FutureDateOrShortDescription_ShouldThrowValidation(string problem, string field)
    {
        // Arrange
        var command = problem == "futura"
            ? new CreateTriggerCommand(7, "Reunión con el gerente", "meeting", 8, DateTime.UtcNow.AddHours(1))
            : new CreateTriggerCommand(7, "abc", "meeting", 8, DateTime.UtcNow.AddHours(-1));

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => CreateService().Handle(command));

        // Assert
        Assert.True(exception.Errors!.ContainsKey(field));
        _triggers.Verify(t => t.AddAsync(It.IsAny<StressTrigger>()), Times.Never);
    }
}
