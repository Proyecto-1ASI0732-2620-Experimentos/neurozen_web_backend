using Moq;
using neurozen.API.Appointments.Interfaces.ACL;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Application.Internal.CommandServices;
using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.Entities;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Reviews;

public class ReviewCommandServiceTests
{
    private readonly Mock<IReviewRepository> _reviews = new();
    private readonly Mock<IProfessionalRepository> _professionals = new();
    private readonly Mock<IAppointmentsContextFacade> _appointments = new();
    private readonly Mock<IIamContextFacade> _iam = new();
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork =
        TestEntities.PassThroughUnitOfWork();
    private readonly neurozen.API.Professionals.Domain.Model.Aggregates.Professional _professional =
        TestProfessionals.Verified().WithId(1);

    public ReviewCommandServiceTests()
    {
        _professionals.Setup(p => p.FindByIdAsync(1)).ReturnsAsync(_professional);
        _reviews.Setup(r => r.ExistsByAppointmentIdAsync(It.IsAny<long>())).ReturnsAsync(false);
        _iam.Setup(i => i.FetchUserSummaryAsync(7)).ReturnsAsync(new UserSummary(7, "Ana Torres", "ana@neurozen.com", false));
    }

    private ReviewCommandService CreateService() =>
        new(_reviews.Object, _professionals.Object, _appointments.Object, _iam.Object, new FakeCurrentUser(7, "USER"),
            _unitOfWork.Object);

    private void AppointmentIs(string status, bool reviewed) =>
        _appointments.Setup(a => a.FindAppointmentAsync(502))
            .ReturnsAsync(new AppointmentReference(502, 7, 1, status, reviewed));

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: tras una sesión atendida el paciente califica; la calificación pública
    ///   (promedio con 1 decimal) y la cantidad de reseñas deben recalcularse, y la cita queda marcada como calificada.
    /// - ¿Qué hace esta prueba?: califica una cita COMPLETED con un promedio almacenado de 4.75 en 4 reseñas y
    ///   comprueba rating 4.8, reviews 4, la reseña insertada una vez y la cita marcada una vez.
    /// </summary>
    [Fact]
    public async Task Review_CompletedAppointment_ShouldRecalculateRatingAndMarkReviewed()
    {
        // Arrange
        AppointmentIs("COMPLETED", false);
        _reviews.Setup(r => r.GetStatisticsAsync(1)).ReturnsAsync(new ReviewStatistics(4.75m, 4));

        // Act
        var result = await CreateService().Handle(new CreateReviewCommand(1, 502, 5, "Muy útil"));

        // Assert
        Assert.Equal(4.8m, _professional.Rating);
        Assert.Equal(4, _professional.ReviewsCount);
        Assert.Equal("Ana T.", result.UserName);
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Once);
        _appointments.Verify(a => a.MarkReviewedAsync(502), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: solo se califica una sesión que ocurrió (COMPLETED) y una sola vez; si no,
    ///   se podría inflar o hundir la calificación de un psicólogo.
    /// - ¿Qué hace esta prueba?: califica una cita CONFIRMED (400 «Solo se puede calificar una sesión atendida») y
    ///   una COMPLETED ya calificada (409); en ningún caso se inserta la reseña ni se marca la cita.
    /// </summary>
    [Theory]
    [InlineData("no atendida")]
    [InlineData("segunda vez")]
    public async Task Review_NotCompletedOrSecondTime_ShouldBeRejected(string scenario)
    {
        // Arrange
        if (scenario == "no atendida") AppointmentIs("CONFIRMED", false);
        else AppointmentIs("COMPLETED", true);

        // Act
        var exception = await Record.ExceptionAsync(
            () => CreateService().Handle(new CreateReviewCommand(1, 502, 5, "Muy útil")));

        // Assert
        if (scenario == "no atendida")
            Assert.Equal("Solo se puede calificar una sesión atendida",
                Assert.IsType<ValidationFailedException>(exception).Message);
        else
            Assert.IsType<ConflictException>(exception);
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
        _appointments.Verify(a => a.MarkReviewedAsync(It.IsAny<long>()), Times.Never);
    }
}
