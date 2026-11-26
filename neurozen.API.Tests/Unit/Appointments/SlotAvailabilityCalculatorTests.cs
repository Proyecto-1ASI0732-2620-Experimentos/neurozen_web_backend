using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Shared.Domain.Model.Time;

namespace neurozen.API.Tests.Unit.Appointments;

/// <summary>
///     Reglas de horas libres (una sola lógica para available-slots y availability): días del horario, bloques según la
///     duración, citas PENDING/CONFIRMED con su propia duración, horas pasadas de hoy y día sin horas → [].
/// </summary>
public class SlotAvailabilityCalculatorTests
{
    private static readonly AvailabilitySchedule MondayMorning = new(["monday"], "09:00", "13:00");

    private static DateOnly NextMonday()
    {
        var date = LimaTime.Today().AddDays(1);
        while (date.DayOfWeek != DayOfWeek.Monday) date = date.AddDays(1);
        return date;
    }

    private static string[] Format(IEnumerable<TimeOnly> slots) => slots.Select(s => s.ToString("HH:mm")).ToArray();

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el calendario necesita bloques de 60 minutos por defecto dentro del horario.
    /// - ¿Qué hace esta prueba?: lunes 09:00-13:00 sin citas debe dar 09:00, 10:00, 11:00 y 12:00.
    /// </summary>
    [Fact]
    public void FreeSlots_DayInSchedule_ShouldReturnHourlyBlocks()
    {
        // Act
        var slots = SlotAvailabilityCalculator.FreeSlots(MondayMorning, NextMonday(), 60, [], DateTime.UtcNow);

        // Assert
        Assert.Equal(["09:00", "10:00", "11:00", "12:00"], Format(slots));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un día que no está en availabilitySchedule.days no tiene horas.
    /// - ¿Qué hace esta prueba?: pide el martes siguiente a un lunes y comprueba la lista vacía.
    /// </summary>
    [Fact]
    public void FreeSlots_DayOutsideSchedule_ShouldReturnEmpty()
    {
        // Act
        var slots = SlotAvailabilityCalculator.FreeSlots(MondayMorning, NextMonday().AddDays(1), 60, [],
            DateTime.UtcNow);

        // Assert
        Assert.Empty(slots);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: sin horario configurado, o en una fecha pasada, no hay horas.
    /// - ¿Qué hace esta prueba?: horario nulo y ayer → listas vacías.
    /// </summary>
    [Fact]
    public void FreeSlots_NoScheduleOrPastDate_ShouldReturnEmpty()
    {
        // Act & Assert
        Assert.Empty(SlotAvailabilityCalculator.FreeSlots(null, NextMonday(), 60, [], DateTime.UtcNow));
        Assert.Empty(SlotAvailabilityCalculator.FreeSlots(TestAllWeek(), LimaTime.Today().AddDays(-1), 60, [],
            DateTime.UtcNow));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: una cita ocupa su propia duración; una de 30 min a las 10:30 bloquea el
    ///   bloque de 60 min de las 10:00, y una de 60 min a las 11:00 bloquea los bloques de 30 min de 11:00 y 11:30.
    /// - ¿Qué hace esta prueba?: combina citas de distinta duración y comprueba los bloques libres de 60 y 30 minutos.
    /// </summary>
    [Fact]
    public void FreeSlots_ShouldExcludeBlocksOverlappingBookedAppointments()
    {
        // Arrange
        var monday = NextMonday();
        var booked = new[]
        {
            BookedInterval.FromStart(LimaTime.ToUtc(monday, new TimeOnly(10, 30)), 30),
            BookedInterval.FromStart(LimaTime.ToUtc(monday, new TimeOnly(11, 0)), 60)
        };

        // Act
        var hourly = SlotAvailabilityCalculator.FreeSlots(MondayMorning, monday, 60, booked, DateTime.UtcNow);
        var halfHour = SlotAvailabilityCalculator.FreeSlots(MondayMorning, monday, 30, booked, DateTime.UtcNow);

        // Assert
        Assert.Equal(["09:00", "12:00"], Format(hourly));
        Assert.Equal(["09:00", "09:30", "10:00", "12:00", "12:30"], Format(halfHour));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: hoy no se pueden ofrecer horas que ya empezaron.
    /// - ¿Qué hace esta prueba?: fija "ahora" a las 10:15 de Lima y comprueba que solo quedan las horas posteriores.
    /// </summary>
    [Fact]
    public void FreeSlots_Today_ShouldExcludePastHours()
    {
        // Arrange
        var today = LimaTime.Today();
        var schedule = new AvailabilitySchedule(AvailabilitySchedule.WeekDays, "09:00", "13:00");
        var nowUtc = LimaTime.ToUtc(today, new TimeOnly(10, 15));

        // Act
        var slots = SlotAvailabilityCalculator.FreeSlots(schedule, today, 60, [], nowUtc);

        // Assert
        Assert.Equal(["11:00", "12:00"], Format(slots));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el bloque debe terminar antes o justo al final del horario.
    /// - ¿Qué hace esta prueba?: 09:00-10:30 con bloques de 45 min da 09:00 y 09:45; con 120 min, ninguno.
    /// </summary>
    [Fact]
    public void FreeSlots_BlocksMustFitBeforeTheEnd()
    {
        // Arrange
        var schedule = new AvailabilitySchedule(["monday"], "09:00", "10:30");

        // Act & Assert
        Assert.Equal(["09:00", "09:45"],
            Format(SlotAvailabilityCalculator.FreeSlots(schedule, NextMonday(), 45, [], DateTime.UtcNow)));
        Assert.Empty(SlotAvailabilityCalculator.FreeSlots(schedule, NextMonday(), 120, [], DateTime.UtcNow));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la reserva acepta inicios del bloque del tipo de cita o de la grilla de 60
    ///   min (compatibilidad con el frontend actual), siempre que la sesión termine dentro del horario.
    /// - ¿Qué hace esta prueba?: valida inicios correctos, fuera de grilla, con segundos y fuera del día.
    /// </summary>
    [Theory]
    [InlineData(9, 0, 60, true)]
    [InlineData(12, 0, 60, true)]
    [InlineData(12, 30, 30, true)]
    [InlineData(10, 0, 45, true)]
    [InlineData(9, 45, 45, true)]
    [InlineData(9, 30, 60, false)]
    [InlineData(13, 0, 30, false)]
    [InlineData(12, 30, 60, false)]
    public void IsScheduledStart_ShouldAcceptOnlyBlockStartsThatFit(int hour, int minute, int duration, bool expected)
    {
        // Arrange
        var startUtc = LimaTime.ToUtc(NextMonday(), new TimeOnly(hour, minute));

        // Act & Assert
        Assert.Equal(expected, SlotAvailabilityCalculator.IsScheduledStart(MondayMorning, startUtc, duration));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un inicio con segundos, en otro día o sin horario no es reservable.
    /// - ¿Qué hace esta prueba?: comprueba esos tres casos y los límites de duración aceptados (15-240).
    /// </summary>
    [Fact]
    public void IsScheduledStart_InvalidCases_And_DurationLimits()
    {
        // Arrange
        var monday9 = LimaTime.ToUtc(NextMonday(), new TimeOnly(9, 0));

        // Act & Assert
        Assert.False(SlotAvailabilityCalculator.IsScheduledStart(MondayMorning, monday9.AddSeconds(5), 60));
        Assert.False(SlotAvailabilityCalculator.IsScheduledStart(MondayMorning, monday9.AddDays(1), 60));
        Assert.False(SlotAvailabilityCalculator.IsScheduledStart(null, monday9, 60));
        Assert.True(SlotAvailabilityCalculator.IsValidDuration(15));
        Assert.True(SlotAvailabilityCalculator.IsValidDuration(240));
        Assert.False(SlotAvailabilityCalculator.IsValidDuration(14));
        Assert.False(SlotAvailabilityCalculator.IsValidDuration(241));
        Assert.Empty(MondayMorning.SlotsFor(DayOfWeek.Monday, 0));
    }

    private static AvailabilitySchedule TestAllWeek() => new(AvailabilitySchedule.WeekDays, "09:00", "13:00");
}
