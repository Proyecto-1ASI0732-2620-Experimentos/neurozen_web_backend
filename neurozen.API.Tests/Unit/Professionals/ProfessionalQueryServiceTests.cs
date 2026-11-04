using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Moq;
using neurozen.API.Appointments.Interfaces.ACL;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Application.Internal.QueryServices;
using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Professionals.Infrastructure.Repositories;
using neurozen.API.Shared.Domain.Model.Time;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Professionals;

public class ProfessionalQueryServiceTests
{
    private readonly Mock<IIamContextFacade> _iam = new();
    private readonly Mock<IAppointmentsContextFacade> _appointments = new();

    public ProfessionalQueryServiceTests()
    {
        _iam.Setup(i => i.FetchUserSummariesAsync(It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(new Dictionary<long, UserSummary>());
    }

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un paciente solo debe poder ver (y reservar con) psicólogos verificados;
    ///   mostrar perfiles PENDING, REJECTED o eliminados expondría a profesionales no validados ante el CPsP.
    /// - ¿Qué hace esta prueba?: guarda cuatro perfiles (verificado, pendiente, rechazado y verificado eliminado) en
    ///   una base en memoria, consulta el directorio con el repositorio real y comprueba que solo aparece el verificado.
    /// </summary>
    [Fact]
    public async Task Directory_ShouldOnlyReturnVerifiedProfessionals()
    {
        // Arrange
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"directorio-{Guid.NewGuid()}").Options);
        var verified = TestProfessionals.Verified(1, "10001");
        var pending = TestProfessionals.Pending(2, "10002");
        var rejected = TestProfessionals.Pending(3, "10003");
        rejected.Reject("La colegiatura no figura como HABILITADO", DateTime.UtcNow);
        var deleted = TestProfessionals.Verified(4, "10004");
        deleted.MarkDeleted(DateTime.UtcNow);
        context.AddRange(verified, pending, rejected, deleted);
        await context.SaveChangesAsync();
        var service = new ProfessionalQueryService(new ProfessionalRepository(context), _iam.Object,
            _appointments.Object);

        // Act
        var directory = await service.Handle(new GetAllProfessionalsQuery());

        // Assert
        var single = Assert.Single(directory);
        Assert.Equal(verified.Id, single.Professional.Id);
        Assert.Equal(VerificationStatus.Verified, single.Professional.VerificationStatus);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el selector de horarios no puede ofrecer horas ya reservadas (doble
    ///   reserva) ni horas que ya pasaron hoy.
    /// - ¿Qué hace esta prueba?: con disponibilidad de 00:00 a 23:59 todos los días, pide los horarios de mañana con
    ///   las 10:00 reservadas (no deben aparecer) y los de hoy (ninguno puede haber pasado; las 00:00 nunca aparecen).
    /// </summary>
    [Fact]
    public async Task AvailableSlots_ShouldExcludeBookedAndPastHours()
    {
        // Arrange
        var professional = TestProfessionals.Verified(schedule: TestProfessionals.AllWeek("00:00", "23:59"))
            .WithId(5);
        var professionals = new Mock<IProfessionalRepository>();
        professionals.Setup(p => p.FindByIdAsync(5)).ReturnsAsync(professional);
        var today = LimaTime.Today();
        var tomorrow = today.AddDays(1);
        _appointments.Setup(a => a.ListBookedStartTimesAsync(5, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<DateTime> { LimaTime.ToUtc(tomorrow, new TimeOnly(10, 0)) });
        var service = new ProfessionalQueryService(professionals.Object, _iam.Object, _appointments.Object);
        var nowBeforeQuery = DateTime.UtcNow;

        // Act
        var tomorrowSlots = await service.Handle(new GetAvailableSlotsQuery(5, Format(tomorrow)));
        var todaySlots = await service.Handle(new GetAvailableSlotsQuery(5, Format(today)));

        // Assert
        Assert.Contains("09:00", tomorrowSlots);
        Assert.Contains("11:00", tomorrowSlots);
        Assert.DoesNotContain("10:00", tomorrowSlots);
        Assert.DoesNotContain("00:00", todaySlots);
        Assert.All(todaySlots, slot =>
            Assert.True(LimaTime.ToUtc(today, TimeOnly.ParseExact(slot, "HH:mm", CultureInfo.InvariantCulture))
                        > nowBeforeQuery));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: si el psicólogo no atiende ese día de la semana, no se deben ofrecer
    ///   horarios, aunque la fecha sea futura.
    /// - ¿Qué hace esta prueba?: con disponibilidad solo los lunes, pide los horarios de un domingo futuro y comprueba
    ///   la lista vacía, sin consultar siquiera las citas.
    /// </summary>
    [Fact]
    public async Task AvailableSlots_DayOutsideSchedule_ShouldReturnEmpty()
    {
        // Arrange
        var mondaysOnly = new AvailabilitySchedule(["monday"], "09:00", "13:00");
        var professional = TestProfessionals.Verified(schedule: mondaysOnly).WithId(5);
        var professionals = new Mock<IProfessionalRepository>();
        professionals.Setup(p => p.FindByIdAsync(5)).ReturnsAsync(professional);
        var sunday = LimaTime.Today().AddDays(1);
        while (sunday.DayOfWeek != DayOfWeek.Sunday) sunday = sunday.AddDays(1);
        var service = new ProfessionalQueryService(professionals.Object, _iam.Object, _appointments.Object);

        // Act
        var slots = await service.Handle(new GetAvailableSlotsQuery(5, Format(sunday)));

        // Assert
        Assert.Empty(slots);
        _appointments.Verify(a => a.ListBookedStartTimesAsync(It.IsAny<long>(), It.IsAny<DateTime>(),
            It.IsAny<DateTime>()), Times.Never);
    }
}
