using Moq;
using neurozen.API.Appointments.Application.Internal;
using neurozen.API.Appointments.Application.Internal.CommandServices;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.Commands;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Appointments.Domain.Repositories;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Appointments;

public class AppointmentCommandServiceTests
{
    private const long PatientId = 7;
    private const long PsychologistUserId = 20;
    private const long ProfessionalId = 3;

    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IProfessionalsContextFacade> _professionals = new();
    private readonly Mock<IIamContextFacade> _iam = new();
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork =
        TestEntities.PassThroughUnitOfWork();

    public AppointmentCommandServiceTests()
    {
        _iam.Setup(i => i.FetchUserSummariesAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new Dictionary<long, UserSummary>
            {
                [PatientId] = new(PatientId, "Ana Torres", "ana@neurozen.com", false)
            });
        _professionals.Setup(p => p.FindByIdAsync(ProfessionalId))
            .ReturnsAsync(new ProfessionalReference(ProfessionalId, PsychologistUserId, true));
        _professionals.Setup(p => p.IsWithinAvailabilityAsync(ProfessionalId, It.IsAny<DateTime>())).ReturnsAsync(true);
        _appointments.Setup(a => a.IsSlotTakenAsync(It.IsAny<long>(), It.IsAny<DateTime>())).ReturnsAsync(false);
    }

    private AppointmentCommandService CreateService(FakeCurrentUser caller) =>
        new(_appointments.Object, _professionals.Object,
            new AppointmentAccess(caller, _professionals.Object, _iam.Object), caller, _unitOfWork.Object);

    private static FakeCurrentUser Patient() => new(PatientId, "USER");
    private static FakeCurrentUser Psychologist() => new(PsychologistUserId, "PSYCHOLOGIST");

    private Appointment ExistingAppointment(DateTime? startUtc = null)
    {
        var appointment = new Appointment(PatientId, ProfessionalId, startUtc ?? DateTime.UtcNow.AddDays(1),
            EAppointmentType.TerapiaIndividual, "Primera sesión").WithId(100);
        _appointments.Setup(a => a.FindByIdAsync(100)).ReturnsAsync(appointment);
        return appointment;
    }

    private static CreateAppointmentCommand Booking(DateTime startUtc) =>
        new(PatientId, ProfessionalId, startUtc, 1, "Me cuesta desconectarme del trabajo");

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: es el camino feliz de la reserva (US14): profesional verificado, fecha
    ///   futura y horario libre deben crear la cita PENDING a nombre del paciente autenticado.
    /// - ¿Qué hace esta prueba?: reserva y comprueba estado, paciente, profesional y que el horario queda ocupado,
    ///   con una sola inserción y un solo guardado.
    /// </summary>
    [Fact]
    public async Task Book_WithVerifiedProfessionalFutureFreeSlot_ShouldCreatePending()
    {
        // Arrange
        var startUtc = DateTime.UtcNow.AddDays(1);
        Appointment? saved = null;
        _appointments.Setup(a => a.AddAsync(It.IsAny<Appointment>()))
            .Callback<Appointment>(a => saved = a).Returns(Task.CompletedTask);

        // Act
        var result = await CreateService(Patient()).Handle(Booking(startUtc));

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(AppointmentStatus.Pending, result.Appointment.Status);
        Assert.Equal(PatientId, saved!.PatientId);
        Assert.Equal(ProfessionalId, saved.ProfessionalId);
        Assert.Equal(startUtc, saved.ActiveSlot);
        Assert.False(saved.Reviewed);
        _appointments.Verify(a => a.AddAsync(It.IsAny<Appointment>()), Times.Once);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un psicólogo no verificado no puede recibir citas; para el paciente ese
    ///   profesional «no existe» (404), sin revelar su estado.
    /// - ¿Qué hace esta prueba?: reserva con un profesional no reservable y comprueba el 404 sin crear la cita.
    /// </summary>
    [Fact]
    public async Task Book_WithPendingProfessional_ShouldThrowNotFound()
    {
        // Arrange
        _professionals.Setup(p => p.FindByIdAsync(ProfessionalId))
            .ReturnsAsync(new ProfessionalReference(ProfessionalId, PsychologistUserId, false));

        // Act
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => CreateService(Patient()).Handle(Booking(DateTime.UtcNow.AddDays(1))));

        // Assert
        _appointments.Verify(a => a.AddAsync(It.IsAny<Appointment>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: no se puede reservar una sesión en el pasado.
    /// - ¿Qué hace esta prueba?: reserva para hace una hora y comprueba el 400, sin consultar la disponibilidad ni
    ///   crear la cita.
    /// </summary>
    [Fact]
    public async Task Book_PastDate_ShouldThrowValidation()
    {
        // Arrange
        var service = CreateService(Patient());

        // Act
        await Assert.ThrowsAsync<ValidationFailedException>(
            () => service.Handle(Booking(DateTime.UtcNow.AddHours(-1))));

        // Assert
        _professionals.Verify(p => p.IsWithinAvailabilityAsync(It.IsAny<long>(), It.IsAny<DateTime>()), Times.Never);
        _appointments.Verify(a => a.AddAsync(It.IsAny<Appointment>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: doble reserva; dos pacientes no pueden tener la misma hora con el mismo
    ///   psicólogo.
    /// - ¿Qué hace esta prueba?: simula un horario ya tomado y comprueba el 409 «El horario ya no está disponible»
    ///   sin crear la cita.
    /// </summary>
    [Fact]
    public async Task Book_AlreadyTakenSlot_ShouldThrowConflict()
    {
        // Arrange
        _appointments.Setup(a => a.IsSlotTakenAsync(ProfessionalId, It.IsAny<DateTime>())).ReturnsAsync(true);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => CreateService(Patient()).Handle(Booking(DateTime.UtcNow.AddDays(1))));

        // Assert
        Assert.Equal("El horario ya no está disponible", exception.Message);
        _appointments.Verify(a => a.AddAsync(It.IsAny<Appointment>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el psicólogo de la cita es quien la confirma (PENDING → CONFIRMED).
    /// - ¿Qué hace esta prueba?: el psicólogo dueño confirma una cita futura y se comprueba el nuevo estado guardado.
    /// </summary>
    [Fact]
    public async Task Confirm_ByOwnPsychologist_ShouldChangePendingToConfirmed()
    {
        // Arrange
        var appointment = ExistingAppointment();

        // Act
        await CreateService(Psychologist()).Handle(new UpdateAppointmentStatusCommand(100, "CONFIRMED"));

        // Assert
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: una sesión solo puede marcarse como atendida cuando su hora ya pasó; si
    ///   no, se podrían calificar sesiones que no ocurrieron.
    /// - ¿Qué hace esta prueba?: el psicólogo intenta completar una cita confirmada de mañana y se comprueba el 400
    ///   «La cita aún no ha ocurrido» con el estado intacto.
    /// </summary>
    [Fact]
    public async Task Complete_BeforeAppointmentDate_ShouldBeRejected()
    {
        // Arrange
        var appointment = ExistingAppointment();
        appointment.Confirm(DateTime.UtcNow);

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => CreateService(Psychologist()).Handle(new UpdateAppointmentStatusCommand(100, "COMPLETED")));

        // Assert
        Assert.Equal("La cita aún no ha ocurrido", exception.Message);
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el paciente puede cancelar su propia cita futura, y el horario debe
    ///   quedar libre para otros.
    /// - ¿Qué hace esta prueba?: el paciente cancela y se comprueba el estado CANCELLED y que la cita ya no ocupa el
    ///   horario.
    /// </summary>
    [Fact]
    public async Task Cancel_ByPatient_ShouldBeAllowed()
    {
        // Arrange
        var appointment = ExistingAppointment();

        // Act
        await CreateService(Patient()).Handle(new UpdateAppointmentStatusCommand(100, "CANCELLED"));

        // Assert
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Null(appointment.ActiveSlot);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: confirmar y marcar como atendida son acciones del psicólogo; si el
    ///   paciente pudiera hacerlas, podría calificar sesiones que no tuvo.
    /// - ¿Qué hace esta prueba?: el paciente intenta CONFIRMED y COMPLETED; ambos dan 403 y la cita sigue PENDING.
    /// </summary>
    [Theory]
    [InlineData("CONFIRMED")]
    [InlineData("COMPLETED")]
    public async Task Patient_TryingToConfirmOrComplete_ShouldBeForbidden(string status)
    {
        // Arrange
        var appointment = ExistingAppointment();

        // Act
        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => CreateService(Patient()).Handle(new UpdateAppointmentStatusCommand(100, status)));

        // Assert
        Assert.Equal(AppointmentStatus.Pending, appointment.Status);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: CANCELLED y COMPLETED son estados finales; reabrir una cita cancelada o
    ///   cancelar una ya atendida rompería la historia y las reseñas.
    /// - ¿Qué hace esta prueba?: el psicólogo intenta confirmar una cita cancelada y cancelar una atendida; ambos
    ///   cambios dan 400 y el estado final se mantiene.
    /// </summary>
    [Theory]
    [InlineData("cancelada", "CONFIRMED")]
    [InlineData("atendida", "CANCELLED")]
    public async Task ChangeStatus_FromFinalState_ShouldBeRejected(string finalState, string requestedStatus)
    {
        // Arrange
        var appointment = ExistingAppointment(DateTime.UtcNow.AddHours(1));
        if (finalState == "cancelada")
        {
            appointment.Cancel(DateTime.UtcNow);
        }
        else
        {
            appointment.Confirm(DateTime.UtcNow);
            appointment.Complete(DateTime.UtcNow.AddHours(2));
        }

        var statusBefore = appointment.Status;

        // Act
        await Assert.ThrowsAsync<ValidationFailedException>(
            () => CreateService(Psychologist()).Handle(new UpdateAppointmentStatusCommand(100, requestedStatus)));

        // Assert
        Assert.Equal(statusBefore, appointment.Status);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }
}
