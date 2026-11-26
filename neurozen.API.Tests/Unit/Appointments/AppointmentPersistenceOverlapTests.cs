using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Appointments.Infrastructure.Repositories;
using neurozen.API.Appointments.Interfaces.ACL.Services;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Infrastructure.Repositories;
using neurozen.API.Professionals.Interfaces.ACL.Services;
using neurozen.API.Shared.Domain.Model.Time;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Appointments;

/// <summary>
///     Consultas de ocupación con base en memoria: solapamiento según la duración de cada tipo, citas canceladas que no
///     ocupan y la grilla del profesional vista desde la fachada.
/// </summary>
public class AppointmentPersistenceOverlapTests
{
    private const long ProfessionalId = 3;

    private static AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"solapamiento-{Guid.NewGuid()}").Options);

    private static DateTime At(int hour, int minute = 0) =>
        LimaTime.ToUtc(LimaTime.Today().AddDays(3), new TimeOnly(hour, minute));

    private static async Task SeedAsync(AppDbContext context)
    {
        var cancelled = new Appointment(8, ProfessionalId, At(14), EAppointmentType.TerapiaIndividual, null);
        cancelled.Cancel(DateTime.UtcNow);
        context.AddRange(
            new Appointment(7, ProfessionalId, At(10), EAppointmentType.TerapiaIndividual, null),
            new Appointment(7, ProfessionalId, At(12), EAppointmentType.Seguimiento, null),
            new Appointment(7, 99, At(16), EAppointmentType.TerapiaIndividual, null),
            cancelled);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la reserva debe detectar solapamientos aunque los inicios sean distintos.
    /// - ¿Qué hace esta prueba?: con citas 10:00 (60 min) y 12:00 (30 min) comprueba qué intervalos chocan; la cita
    ///   cancelada de las 14:00 y la de otro profesional no cuentan.
    /// </summary>
    [Theory]
    [InlineData(10, 30, 30, true)]
    [InlineData(9, 30, 45, true)]
    [InlineData(11, 0, 60, false)]
    [InlineData(12, 15, 30, true)]
    [InlineData(12, 30, 60, false)]
    [InlineData(14, 0, 60, false)]
    [InlineData(16, 0, 60, false)]
    public async Task Repository_HasOverlappingActiveAppointment(int hour, int minute, int duration, bool expected)
    {
        // Arrange
        await using var context = NewContext();
        await SeedAsync(context);
        var repository = new AppointmentRepository(context);

        // Act
        var overlaps = await repository.HasOverlappingActiveAppointmentAsync(ProfessionalId, At(hour, minute), duration);

        // Assert
        Assert.Equal(expected, overlaps);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: fuera de una transacción relacional el bloqueo no hace nada (no falla).
    /// - ¿Qué hace esta prueba?: llama al bloqueo con el proveedor en memoria.
    /// </summary>
    [Fact]
    public async Task Repository_Lock_IsNoOpOutsideRelationalTransaction()
    {
        // Arrange
        await using var context = NewContext();

        // Act
        var exception = await Record.ExceptionAsync(() =>
            new AppointmentRepository(context).LockProfessionalScheduleAsync(ProfessionalId));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: available-slots necesita cada cita ocupada con su duración, incluida una
    ///   que empezó antes del rango y sigue en curso.
    /// - ¿Qué hace esta prueba?: pide el rango 10:30-13:00 y espera la de las 10:00 (60) y la de las 12:00 (30); sin la
    ///   cancelada ni la de otro profesional. También comprueba el método antiguo de inicios.
    /// </summary>
    [Fact]
    public async Task Facade_ListBookedSlots_IncludesRunningAppointmentsWithDuration()
    {
        // Arrange
        await using var context = NewContext();
        await SeedAsync(context);
        var facade = new AppointmentsContextFacade(context);

        // Act
        var slots = await facade.ListBookedSlotsAsync(ProfessionalId, At(10, 30), At(13));
        var starts = await facade.ListBookedStartTimesAsync(ProfessionalId, At(0), At(23));

        // Assert
        Assert.Equal([(At(10), 60), (At(12), 30)], slots.Select(s => (s.StartUtc, s.DurationMinutes)));
        Assert.Equal(2, starts.Count);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la fachada valida el inicio contra la grilla del profesional según la
    ///   duración del tipo de cita.
    /// - ¿Qué hace esta prueba?: 09:00-13:00 todos los días: 10:00 vale para 60; 10:30 solo para 30; un profesional
    ///   inexistente nunca.
    /// </summary>
    [Fact]
    public async Task ProfessionalsFacade_IsWithinAvailability_ByDuration()
    {
        // Arrange
        await using var context = NewContext();
        var professional = TestProfessionals.Verified(schedule: new AvailabilitySchedule(AvailabilitySchedule.WeekDays,
            "09:00", "13:00"));
        context.Add(professional);
        await context.SaveChangesAsync();
        var facade = new ProfessionalsContextFacade(new ProfessionalRepository(context),
            NullLogger<ProfessionalsContextFacade>.Instance);

        // Act & Assert
        Assert.True(await facade.IsWithinAvailabilityAsync(professional.Id, At(10)));
        Assert.False(await facade.IsWithinAvailabilityAsync(professional.Id, At(10, 30)));
        Assert.True(await facade.IsWithinAvailabilityAsync(professional.Id, At(10, 30), 30));
        Assert.False(await facade.IsWithinAvailabilityAsync(12345, At(10), 60));
    }
}
