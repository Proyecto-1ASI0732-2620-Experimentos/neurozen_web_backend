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

/// <summary>
///     Reglas añadidas a la reserva: 409 fuera de horario, 409 por solapamiento con citas de otra duración, inicio en la
///     grilla propia del tipo de cita y verificación dentro de una transacción con bloqueo del profesional.
/// </summary>
public class AppointmentBookingRulesTests
{
    private const long PatientId = 7;
    private const long ProfessionalId = 3;

    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IProfessionalsContextFacade> _professionals = new();
    private readonly Mock<IIamContextFacade> _iam = new();
    private readonly CapturedValue<bool> _insideTransaction = new(false);
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork;

    public AppointmentBookingRulesTests()
    {
        _unitOfWork = TestEntities.PassThroughUnitOfWork(_insideTransaction);
        _iam.Setup(i => i.FetchUserSummariesAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new Dictionary<long, UserSummary>());
        _professionals.Setup(p => p.FindByIdAsync(ProfessionalId))
            .ReturnsAsync(new ProfessionalReference(ProfessionalId, 20, true));
    }

    private AppointmentCommandService Service()
    {
        var caller = new FakeCurrentUser(PatientId, "USER");
        return new AppointmentCommandService(_appointments.Object, _professionals.Object,
            new AppointmentAccess(caller, _professionals.Object, _iam.Object), caller, _unitOfWork.Object);
    }

    private static CreateAppointmentCommand Booking(DateTime startUtc, int type = 1) =>
        new(PatientId, ProfessionalId, startUtc, type, null);

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el frontend vuelve a pedir otra hora sin perder datos cuando recibe 409; una
    ///   hora fuera del horario del profesional debe responder 409 (antes era 400).
    /// - ¿Qué hace esta prueba?: simula una hora fuera de la disponibilidad y comprueba el 409 sin crear la cita.
    /// </summary>
    [Fact]
    public async Task Book_OutsideAvailability_ShouldThrowConflict()
    {
        // Arrange
        _professionals.Setup(p => p.IsWithinAvailabilityAsync(ProfessionalId, It.IsAny<DateTime>())).ReturnsAsync(false);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            Service().Handle(Booking(DateTime.UtcNow.AddDays(1).Date.AddHours(15))));

        // Assert
        Assert.Equal(AppointmentCommandService.OutsideAvailabilityMessage, exception.Message);
        _appointments.Verify(a => a.AddAsync(It.IsAny<Appointment>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: una cita no puede solaparse con otra activa aunque empiece a otra hora (por
    ///   ejemplo, una de 30 min dentro de una sesión de 60 min).
    /// - ¿Qué hace esta prueba?: el repositorio informa un solapamiento; se espera 409 «El horario ya no está
    ///   disponible» y que la verificación ocurra dentro de la transacción, tras bloquear el horario del profesional.
    /// </summary>
    [Fact]
    public async Task Book_OverlappingAnotherAppointment_ShouldThrowConflictInsideTransaction()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(15).AddMinutes(30);
        _professionals.Setup(p => p.IsWithinAvailabilityAsync(ProfessionalId, start)).ReturnsAsync(false);
        _professionals.Setup(p => p.IsWithinAvailabilityAsync(ProfessionalId, start, 30)).ReturnsAsync(true);
        var lockedInsideTransaction = false;
        _appointments.Setup(a => a.LockProfessionalScheduleAsync(ProfessionalId))
            .Callback(() => lockedInsideTransaction = _insideTransaction.Value).Returns(Task.CompletedTask);
        _appointments.Setup(a => a.HasOverlappingActiveAppointmentAsync(ProfessionalId, start, 30)).ReturnsAsync(true);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(() => Service().Handle(Booking(start, 3)));

        // Assert
        Assert.Equal(AppointmentCommandService.SlotTakenMessage, exception.Message);
        Assert.True(lockedInsideTransaction);
        _appointments.Verify(a => a.AddAsync(It.IsAny<Appointment>()), Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un seguimiento (30 min) puede empezar en su propia grilla (p. ej. 15:30),
    ///   que ofrece available-slots?durationMinutes=30.
    /// - ¿Qué hace esta prueba?: la grilla de 60 min lo rechaza, la de 30 lo acepta; la cita se crea PENDING con el
    ///   tipo Seguimiento.
    /// </summary>
    [Fact]
    public async Task Book_ShortTypeOnItsOwnGrid_ShouldCreatePending()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(15).AddMinutes(30);
        _professionals.Setup(p => p.IsWithinAvailabilityAsync(ProfessionalId, start)).ReturnsAsync(false);
        _professionals.Setup(p => p.IsWithinAvailabilityAsync(ProfessionalId, start, 30)).ReturnsAsync(true);

        // Act
        var result = await Service().Handle(Booking(start, 3));

        // Assert
        Assert.Equal(AppointmentStatus.Pending, result.Appointment.Status);
        Assert.Equal(EAppointmentType.Seguimiento, result.Appointment.AppointmentType);
        _appointments.Verify(a => a.AddAsync(It.IsAny<Appointment>()), Times.Once);
        _unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la terapia individual (60 min) no consulta la grilla alternativa: si la
    ///   grilla de 60 la rechaza, es 409.
    /// - ¿Qué hace esta prueba?: comprueba que no se llama a la sobrecarga con duración para el tipo 1.
    /// </summary>
    [Fact]
    public async Task Book_SixtyMinuteType_ShouldOnlyUseTheDefaultGrid()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(15);
        _professionals.Setup(p => p.IsWithinAvailabilityAsync(ProfessionalId, start)).ReturnsAsync(false);

        // Act
        await Assert.ThrowsAsync<ConflictException>(() => Service().Handle(Booking(start)));

        // Assert
        _professionals.Verify(p => p.IsWithinAvailabilityAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<int>()),
            Times.Never);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: las duraciones de los tipos determinan el tiempo ocupado.
    /// - ¿Qué hace esta prueba?: 1 → 60, 2 → 45, 3 → 30 y la mayor duración es 60.
    /// </summary>
    [Fact]
    public void AppointmentType_Durations()
    {
        Assert.Equal(60, AppointmentType.DurationOf(EAppointmentType.TerapiaIndividual));
        Assert.Equal(45, AppointmentType.DurationOf(EAppointmentType.ConsultaInicial));
        Assert.Equal(30, AppointmentType.DurationOf(EAppointmentType.Seguimiento));
        Assert.Equal(60, AppointmentType.MaxDurationMinutes);
        Assert.Same(AppointmentType.TerapiaIndividual, AppointmentType.From((EAppointmentType)99));
    }
}
