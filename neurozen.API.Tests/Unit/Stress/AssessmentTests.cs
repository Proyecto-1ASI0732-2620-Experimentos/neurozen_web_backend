using Moq;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Stress.Application.Internal.CommandServices;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Commands;
using neurozen.API.Stress.Domain.Model.ValueObjects;
using neurozen.API.Stress.Domain.Repositories;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Stress;

public class AssessmentTests
{
    private readonly Mock<IAssessmentRepository> _assessments = new();
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork =
        TestEntities.PassThroughUnitOfWork();

    private AssessmentCommandService CreateService() =>
        new(_assessments.Object, new FakeCurrentUser(7, "USER"), _unitOfWork.Object);

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: en la PSS-10 las preguntas 4, 5, 7 y 8 se puntúan invertidas; sin esa
    ///   inversión el puntaje y el nivel de estrés serían falsos. El puntaje se calcula en el servidor (el comando no
    ///   acepta score ni level del cliente).
    /// - ¿Qué hace esta prueba?: con diez respuestas «3» el puntaje es 6×3 + 4×(4-3) = 22 (moderate), y con 4 en las
    ///   preguntas invertidas y 0 en el resto es 0 (low); comprueba ambos resultados guardados.
    /// </summary>
    [Theory]
    [InlineData(new[] { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }, 22, "moderate")]
    [InlineData(new[] { 0, 0, 0, 4, 4, 0, 4, 4, 0, 0 }, 0, "low")]
    public async Task Assessment_ShouldRecalculateScoreWithReversedQuestions4578(int[] answers, int expectedScore,
        string expectedLevel)
    {
        // Arrange
        Assessment? saved = null;
        _assessments.Setup(a => a.AddAsync(It.IsAny<Assessment>()))
            .Callback<Assessment>(a => saved = a).Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().Handle(new CreateAssessmentCommand(7, answers));

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(expectedScore, result.Score);
        Assert.Equal(expectedLevel, result.Level.ToWire());
        _assessments.Verify(a => a.AddAsync(It.IsAny<Assessment>()), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: los cortes de nivel (0-13 low, 14-26 moderate, 27-40 high) deciden lo que
    ///   ve el usuario; un error de «off by one» en el límite lo clasificaría mal.
    /// - ¿Qué hace esta prueba?: evalúa exactamente los puntajes límite 13, 14, 26 y 27.
    /// </summary>
    [Theory]
    [InlineData(13, AssessmentLevel.Low)]
    [InlineData(14, AssessmentLevel.Moderate)]
    [InlineData(26, AssessmentLevel.Moderate)]
    [InlineData(27, AssessmentLevel.High)]
    public void Assessment_LevelBoundaries_13_14_26_27(int score, AssessmentLevel expected)
    {
        // Arrange
        // (el puntaje límite viene de los datos de la prueba)

        // Act
        var level = PerceivedStressScale.LevelFor(score);

        // Assert
        Assert.Equal(expected, level);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el test solo es válido con exactamente 10 respuestas entre 0 y 4; otra
    ///   cantidad o valores fuera de rango darían un puntaje sin sentido.
    /// - ¿Qué hace esta prueba?: envía 9 y 11 respuestas, un 5, un -1 y ninguna lista; todas dan 400 en answers sin
    ///   guardar nada.
    /// </summary>
    [Theory]
    [InlineData(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 })]
    [InlineData(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 })]
    [InlineData(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 5 })]
    [InlineData(new[] { -1, 1, 1, 1, 1, 1, 1, 1, 1, 1 })]
    [InlineData(null)]
    public async Task Assessment_NotTenAnswersOrOutOfRange_ShouldThrowValidation(int[]? answers)
    {
        // Arrange
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => service.Handle(new CreateAssessmentCommand(7, answers)));

        // Assert
        Assert.True(exception.Errors!.ContainsKey("answers"));
        _assessments.Verify(a => a.AddAsync(It.IsAny<Assessment>()), Times.Never);
    }
}
