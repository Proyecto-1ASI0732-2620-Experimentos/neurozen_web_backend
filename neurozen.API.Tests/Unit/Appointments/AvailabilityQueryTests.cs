using System.Globalization;
using Moq;
using neurozen.API.Appointments.Application.Internal.QueryServices;
using neurozen.API.Appointments.Domain.Model.Queries;
using neurozen.API.Appointments.Interfaces.ACL;
using neurozen.API.Appointments.Interfaces.REST.Transform;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Application.Internal.QueryServices;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Professionals.Interfaces.ACL.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Time;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Appointments;

/// <summary>
///     Horas libres de un día (con durationMinutes) y disponibilidad por rango (límites, nextAvailableDate,
///     autorización), sobre la misma lógica de dominio.
/// </summary>
public class AvailabilityQueryTests
{
    private const long ProfessionalId = 5;
    private readonly Mock<IProfessionalRepository> _professionals = new();
    private readonly Mock<IIamContextFacade> _iam = new();
    private readonly Mock<IAppointmentsContextFacade> _appointments = new();

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly Next(DayOfWeek day)
    {
        var date = LimaTime.Today().AddDays(1);
        while (date.DayOfWeek != day) date = date.AddDays(1);
        return date;
    }

    private ProfessionalQueryService Service(Professional? professional)
    {
        _professionals.Setup(p => p.FindByIdAsync(ProfessionalId)).ReturnsAsync(professional);
        return new ProfessionalQueryService(_professionals.Object, _iam.Object, _appointments.Object);
    }

    private static Professional MondaysOnly() =>
        TestProfessionals.Verified(schedule: new AvailabilitySchedule(["monday"], "09:00", "11:00"))
            .WithId(ProfessionalId);

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el frontend puede pedir bloques de la duración del tipo de cita.
    /// - ¿Qué hace esta prueba?: con ?durationMinutes=30 y una cita de 60 min a las 09:00, solo quedan 10:00 y 10:30.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_WithDuration_ShouldUseBlocksAndOverlap()
    {
        // Arrange
        var monday = Next(DayOfWeek.Monday);
        _appointments.Setup(a => a.ListBookedSlotsAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([new BookedSlotReference(LimaTime.ToUtc(monday, new TimeOnly(9, 0)), 60)]);

        // Act
        var slots = await Service(MondaysOnly()).Handle(new GetAvailableSlotsQuery(ProfessionalId, Format(monday), 30));

        // Assert
        Assert.Equal(["10:00", "10:30"], slots);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: fecha o duración inválidas → 400; profesional no verificado → 404.
    /// - ¿Qué hace esta prueba?: prueba los tres errores de available-slots.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_InvalidInput_ShouldFail()
    {
        // Arrange
        var service = Service(MondaysOnly());

        // Act & Assert
        var badDate = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            service.Handle(new GetAvailableSlotsQuery(ProfessionalId, "21/10/2026")));
        Assert.Equal("La fecha debe tener el formato YYYY-MM-DD", badDate.Message);
        var badDuration = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            service.Handle(new GetAvailableSlotsQuery(ProfessionalId, Format(Next(DayOfWeek.Monday)), 5)));
        Assert.True(badDuration.Errors!.ContainsKey("durationMinutes"));

        var pending = TestProfessionals.Pending().WithId(ProfessionalId);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            Service(pending).Handle(new GetAvailableSlotsQuery(ProfessionalId, Format(Next(DayOfWeek.Monday)))));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el rango devuelve todos los días (con [] los que no tienen horas) y el
    ///   siguiente día disponible, que puede estar después de endDate.
    /// - ¿Qué hace esta prueba?: pide martes a domingo de un psicólogo que solo atiende lunes; todos los días vienen
    ///   vacíos y nextAvailableDate es el lunes siguiente.
    /// </summary>
    [Fact]
    public async Task Range_ShouldListEveryDay_AndFindNextAvailableAfterEnd()
    {
        // Arrange
        var tuesday = Next(DayOfWeek.Tuesday);
        var sunday = tuesday.AddDays(5);

        // Act
        var calendar = await Service(MondaysOnly()).Handle(
            new GetAvailabilityRangeQuery(ProfessionalId, Format(tuesday), Format(sunday)));

        // Assert
        Assert.Equal(6, calendar.Days.Count);
        Assert.All(calendar.Days, day => Assert.Empty(day.Slots));
        Assert.Equal(sunday.AddDays(1), calendar.NextAvailableDate);
        Assert.Equal("America/Lima", calendar.TimeZone);
        Assert.Equal(60, calendar.SlotMinutes);
        _appointments.Verify(a => a.ListBookedSlotsAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Once);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: dentro del rango, nextAvailableDate es el primer día con horas libres, y
    ///   las citas ocupadas se descuentan.
    /// - ¿Qué hace esta prueba?: el lunes tiene 09:00 ocupada; el rango de lunes a martes devuelve ["10:00"] y
    ///   nextAvailableDate = lunes.
    /// </summary>
    [Fact]
    public async Task Range_WithFreeDayInside_ShouldPointToIt()
    {
        // Arrange
        var monday = Next(DayOfWeek.Monday);
        _appointments.Setup(a => a.ListBookedSlotsAsync(ProfessionalId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([new BookedSlotReference(LimaTime.ToUtc(monday, new TimeOnly(9, 0)), 60)]);

        // Act
        var calendar = await Service(MondaysOnly()).Handle(
            new GetAvailabilityRangeQuery(ProfessionalId, Format(monday), Format(monday.AddDays(1))));

        // Assert
        Assert.Equal(["10:00"], calendar.Days[0].Slots);
        Assert.Empty(calendar.Days[1].Slots);
        Assert.Equal(monday, calendar.NextAvailableDate);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un psicólogo sin horario no tiene próximas fechas (null), y un rango que
    ///   terminó hace mucho no consulta citas.
    /// - ¿Qué hace esta prueba?: comprueba nextAvailableDate = null en ambos casos.
    /// </summary>
    [Fact]
    public async Task Range_WithoutScheduleOrFarInThePast_ShouldHaveNoNextDate()
    {
        // Arrange
        var noSchedule = TestProfessionals.Verified().WithId(ProfessionalId);
        typeof(Professional).GetProperty(nameof(Professional.AvailabilitySchedule))!
            .SetValue(noSchedule, null);
        var past = LimaTime.Today().AddDays(-200);

        // Act
        var withoutSchedule = await Service(noSchedule).Handle(new GetAvailabilityRangeQuery(ProfessionalId,
            Format(LimaTime.Today()), Format(LimaTime.Today().AddDays(3))));
        var farPast = await Service(MondaysOnly()).Handle(new GetAvailabilityRangeQuery(ProfessionalId,
            Format(past), Format(past.AddDays(1))));

        // Assert
        Assert.Null(withoutSchedule.NextAvailableDate);
        Assert.Equal(4, withoutSchedule.Days.Count);
        Assert.Null(farPast.NextAvailableDate);
        Assert.All(farPast.Days, day => Assert.Empty(day.Slots));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el rango máximo es de 92 días; fechas inválidas o invertidas → 400.
    /// - ¿Qué hace esta prueba?: 92 días se acepta; 93, endDate &lt; startDate y fechas mal escritas fallan.
    /// </summary>
    [Fact]
    public async Task Range_Limits()
    {
        // Arrange
        var service = Service(MondaysOnly());
        var start = LimaTime.Today();

        // Act
        var ok = await service.Handle(new GetAvailabilityRangeQuery(ProfessionalId, Format(start),
            Format(start.AddDays(91))));

        // Assert
        Assert.Equal(92, ok.Days.Count);
        await Assert.ThrowsAsync<ValidationFailedException>(() => service.Handle(
            new GetAvailabilityRangeQuery(ProfessionalId, Format(start), Format(start.AddDays(92)))));
        await Assert.ThrowsAsync<ValidationFailedException>(() => service.Handle(
            new GetAvailabilityRangeQuery(ProfessionalId, Format(start.AddDays(2)), Format(start))));
        var invalid = await Assert.ThrowsAsync<ValidationFailedException>(() => service.Handle(
            new GetAvailabilityRangeQuery(ProfessionalId, "mañana", null)));
        Assert.True(invalid.Errors!.ContainsKey("startDate"));
        Assert.True(invalid.Errors.ContainsKey("endDate"));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el endpoint de rango es para USER y ADMIN; un PSYCHOLOGIST solo ve su propio
    ///   calendario; professionalId es obligatorio.
    /// - ¿Qué hace esta prueba?: recorre los cuatro casos de autorización con la fachada simulada.
    /// </summary>
    [Fact]
    public async Task RangeEndpointService_ShouldApplyAuthorization()
    {
        // Arrange
        var professionals = new Mock<IProfessionalsContextFacade>();
        professionals.Setup(p => p.FindByUserIdAsync(20)).ReturnsAsync(new ProfessionalReference(ProfessionalId, 20, true));
        var availability = new Mock<IProfessionalAvailabilityFacade>();
        var calendar = new AvailabilityCalendar(ProfessionalId, "America/Lima", 60, [], null);
        availability.Setup(a => a.GetAvailabilityAsync(It.IsAny<long>(), "2026-12-01", "2026-12-02", null))
            .ReturnsAsync(calendar);
        AppointmentAvailabilityQueryService As(long id, string role) =>
            new(professionals.Object, availability.Object, new FakeCurrentUser(id, role));
        var own = new GetAppointmentAvailabilityQuery(ProfessionalId, "2026-12-01", "2026-12-02");
        var other = own with { ProfessionalId = 99 };

        // Act & Assert
        Assert.Same(calendar, await As(7, "USER").Handle(other));
        Assert.Same(calendar, await As(1, "ADMIN").Handle(own));
        Assert.Same(calendar, await As(20, "PSYCHOLOGIST").Handle(own));
        var forbidden = await Assert.ThrowsAsync<ForbiddenOperationException>(() => As(20, "PSYCHOLOGIST").Handle(other));
        Assert.Equal(AppointmentAvailabilityQueryService.OwnAvailabilityOnlyMessage, forbidden.Message);
        await Assert.ThrowsAsync<ForbiddenOperationException>(() => As(30, "PSYCHOLOGIST").Handle(own));
        await Assert.ThrowsAsync<ForbiddenOperationException>(() => As(8, "OTHER").Handle(own));
        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            As(7, "USER").Handle(own with { ProfessionalId = null }));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la fachada de disponibilidad y el ensamblador entregan el contrato pedido
    ///   (fechas "YYYY-MM-DD", nextAvailableDate opcional).
    /// - ¿Qué hace esta prueba?: pasa por la fachada real hasta el servicio y convierte el resultado a recurso REST.
    /// </summary>
    [Fact]
    public async Task Facade_And_Assembler_ShouldProduceTheContract()
    {
        // Arrange
        var monday = Next(DayOfWeek.Monday);
        var facade = new ProfessionalAvailabilityFacade(Service(MondaysOnly()));

        // Act
        var calendar = await facade.GetAvailabilityAsync(ProfessionalId, Format(monday), Format(monday), 60);
        var resource = AvailabilityResourceAssembler.ToResource(calendar);
        var empty = AvailabilityResourceAssembler.ToResource(calendar with { NextAvailableDate = null });

        // Assert
        Assert.Equal(ProfessionalId, resource.ProfessionalId);
        Assert.Equal(Format(monday), resource.Days.Single().Date);
        Assert.Equal(["09:00", "10:00"], resource.Days.Single().Slots);
        Assert.Equal(Format(monday), resource.NextAvailableDate);
        Assert.Null(empty.NextAvailableDate);
    }
}
