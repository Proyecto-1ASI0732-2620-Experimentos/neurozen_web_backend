using Moq;
using neurozen.API.Appointments.Interfaces.ACL;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Stress.Application.Internal.CommandServices;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Commands;
using neurozen.API.Stress.Domain.Model.ValueObjects;
using neurozen.API.Stress.Domain.Repositories;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Stress;

public class ReportCommandServiceTests
{
    private readonly Mock<IReportRepository> _reports = new();
    private readonly Mock<IAppointmentsContextFacade> _appointments = new();
    private readonly Mock<IIamContextFacade> _iam = new();
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork =
        TestEntities.PassThroughUnitOfWork();

    public ReportCommandServiceTests()
    {
        _reports.Setup(r => r.ExistsByAppointmentIdAsync(It.IsAny<long>())).ReturnsAsync(false);
        _iam.Setup(i => i.FetchUserSummaryAsync(7)).ReturnsAsync(new UserSummary(7, "Ana Torres", "ana@neurozen.com", false));
    }

    private ReportCommandService CreateService() =>
        new(_reports.Object, _appointments.Object, _iam.Object, new FakeCurrentUser(7, "USER"), _unitOfWork.Object);

    private static CreateReportCommand Report() =>
        new(7, 1, 501, 70m, 62m, -5m, "15:00 - 17:00", 4, "meeting",
            new LastAssessmentInput(22, DateTime.UtcNow.AddDays(-1)), "Semana de cierre muy pesada");

    private void AppointmentIs(long patientId, long professionalId, string status) =>
        _appointments.Setup(a => a.FindAppointmentAsync(501))
            .ReturnsAsync(new AppointmentReference(501, patientId, professionalId, status, false));

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el paciente comparte su informe (US15) solo con el psicólogo de una cita
    ///   suya activa; el nivel del último test se recalcula desde el puntaje para que sea coherente.
    /// - ¿Qué hace esta prueba?: crea el informe para una cita CONFIRMED propia y comprueba paciente, profesional,
    ///   cita, categoría y nivel guardados.
    /// </summary>
    [Fact]
    public async Task Report_ForOwnActiveAppointment_ShouldBeCreated()
    {
        // Arrange
        AppointmentIs(7, 1, "CONFIRMED");
        Report? saved = null;
        _reports.Setup(r => r.AddAsync(It.IsAny<Report>())).Callback<Report>(r => saved = r).Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().Handle(Report());

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(7, saved!.PatientId);
        Assert.Equal(1, saved.ProfessionalId);
        Assert.Equal(501, saved.AppointmentId);
        Assert.Equal(TriggerCategory.Meeting, saved.TopCategory);
        Assert.Equal(AssessmentLevel.Moderate, saved.LastAssessmentLevel);
        Assert.Equal("Ana Torres", result.PatientName);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: consentimiento y privacidad; nadie puede enviar un informe sobre la cita
    ///   de otro paciente, a otro profesional, ni sobre una cita cancelada.
    /// - ¿Qué hace esta prueba?: prueba una cita de otro paciente, una de otro profesional y una cancelada; todas
    ///   dan 400 y no se guarda ningún informe.
    /// </summary>
    [Theory]
    [InlineData(8, 1, "CONFIRMED")]
    [InlineData(7, 2, "CONFIRMED")]
    [InlineData(7, 1, "CANCELLED")]
    public async Task Report_ForOthersOrCancelledAppointment_ShouldBeRejected(long patientId, long professionalId,
        string status)
    {
        // Arrange
        AppointmentIs(patientId, professionalId, status);

        // Act
        await Assert.ThrowsAsync<ValidationFailedException>(() => CreateService().Handle(Report()));

        // Assert
        _reports.Verify(r => r.AddAsync(It.IsAny<Report>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un informe por cita; un segundo envío duplicaría información de salud.
    /// - ¿Qué hace esta prueba?: con un informe ya existente para la cita comprueba el 409 con su mensaje.
    /// </summary>
    [Fact]
    public async Task Report_SecondForSameAppointment_ShouldThrowConflict()
    {
        // Arrange
        AppointmentIs(7, 1, "CONFIRMED");
        _reports.Setup(r => r.ExistsByAppointmentIdAsync(501)).ReturnsAsync(true);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(() => CreateService().Handle(Report()));

        // Assert
        Assert.Equal("Ya compartiste un informe para esta cita", exception.Message);
        _reports.Verify(r => r.AddAsync(It.IsAny<Report>()), Times.Never);
    }
}
