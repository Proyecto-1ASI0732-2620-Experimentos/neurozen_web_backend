using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Moq;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Appointments.Infrastructure.Repositories;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.ValueObjects;
using neurozen.API.Stress.Infrastructure.Repositories;
using neurozen.API.Tests.Unit.Support;
using neurozen.API.Wellness.Application.Internal.CommandServices;
using neurozen.API.Wellness.Application.Internal.QueryServices;
using neurozen.API.Wellness.Domain.Entities;
using neurozen.API.Wellness.Domain.Model.Commands;
using neurozen.API.Wellness.Domain.Model.Queries;
using neurozen.API.Wellness.Domain.Repositories;
using neurozen.API.Wellness.Infrastructure.Repositories;

namespace neurozen.API.Tests.Unit.Wellness;

public class WellnessServiceTests
{
    [Fact]
    public async Task CreateHealthMetric_ShouldSaveAndCompleteUnitOfWork()
    {
        var repository = new Mock<IHealthMetricRepository>();
        var unitOfWork = TestEntities.PassThroughUnitOfWork();
        HealthMetric? saved = null;
        repository.Setup(r => r.AddAsync(It.IsAny<HealthMetric>()))
            .Callback<HealthMetric>(metric => saved = metric)
            .Returns(Task.CompletedTask);
        var service = new HealthMetricCommandService(repository.Object, unitOfWork.Object);

        var result = await service.Handle(new CreateHealthMetricCommand(7, 5, 80, 7.5m, "descanso"));

        Assert.Same(saved, result);
        Assert.Equal(7, result.UserId);
        unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateHealthMetric_InvalidValues_ShouldNotPersist()
    {
        var repository = new Mock<IHealthMetricRepository>();
        var unitOfWork = TestEntities.PassThroughUnitOfWork();
        var service = new HealthMetricCommandService(repository.Object, unitOfWork.Object);

        var error = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            service.Handle(new CreateHealthMetricCommand(7, 0, 300, 25m, new string('x', 1001))));

        Assert.Contains("stressLevel", error.Errors!.Keys);
        Assert.Contains("heartRate", error.Errors.Keys);
        Assert.Contains("sleepHours", error.Errors.Keys);
        Assert.Contains("notes", error.Errors.Keys);
        repository.Verify(r => r.AddAsync(It.IsAny<HealthMetric>()), Times.Never);
        unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task HealthMetricQuery_ShouldUseCallerAndCapPageSize()
    {
        var repository = new Mock<IHealthMetricRepository>();
        var result = new PageResult<HealthMetric>([], 200);
        repository.Setup(r => r.ListByUserIdAsync(7, new PageRequest(2, 100))).ReturnsAsync(result);
        var service = new HealthMetricQueryService(repository.Object, new FakeCurrentUser(7, "USER"));

        var actual = await service.Handle(new GetHealthMetricsQuery(7, 2, 500));

        Assert.Same(result, actual);
        repository.Verify(r => r.ListByUserIdAsync(7, new PageRequest(2, 100)), Times.Once);
    }

    [Fact]
    public async Task HealthMetricQuery_OtherUser_ShouldBeForbidden()
    {
        var repository = new Mock<IHealthMetricRepository>();
        var service = new HealthMetricQueryService(repository.Object, new FakeCurrentUser(7, "USER"));

        await Assert.ThrowsAsync<ForbiddenOperationException>(() =>
            service.Handle(new GetHealthMetricsQuery(8)));

        repository.Verify(r => r.ListByUserIdAsync(It.IsAny<long>(), It.IsAny<PageRequest?>()), Times.Never);
    }

    [Fact]
    public async Task MeditationQuery_ShouldReturnRepositoryResults()
    {
        var meditations = new[] { new Meditation("Calma", "Descripción", 10, "image", "audio") };
        var repository = new Mock<IMeditationRepository>();
        repository.Setup(r => r.ListAllAsync()).ReturnsAsync(meditations);
        var service = new MeditationQueryService(repository.Object);

        var result = await service.Handle(new GetMeditationsQuery());

        Assert.Same(meditations, result);
        repository.Verify(r => r.ListAllAsync(), Times.Once);
    }
}

public class WellnessAndStressPaginationRepositoryTests
{
    [Fact]
    public async Task Appointments_ShouldFilterOrderAndPageInDatabase()
    {
        await using var context = CreateContext();
        SeedUser(context, 7, "patient@example.test", UserRole.User);
        SeedUser(context, 8, "professional@example.test", UserRole.Psychologist);
        context.Add(new Professional(8, Specialty.Clinical, "12345").WithId(20));
        context.AddRange(
            Appointment(7, 101).WithId(101),
            Appointment(7, 102).WithId(102),
            Appointment(7, 103).WithId(103));
        await context.SaveChangesAsync();

        var repository = new AppointmentRepository(context);
        var byPatient = await repository.ListPageByPatientIdAsync(7, PageRequest.From(2, 1));
        var byProfessional = await repository.ListPageByProfessionalIdAsync(20, PageRequest.From(1, 2));

        Assert.Equal(3, byPatient.TotalCount);
        Assert.Equal(102, Assert.Single(byPatient.Items).Id);
        Assert.Equal(3, byProfessional.TotalCount);
        Assert.Equal(new long[] { 101, 102 }, byProfessional.Items.Select(appointment => appointment.Id));
    }

    [Fact]
    public async Task HealthMetrics_ShouldFilterOrderAndPageInDatabase()
    {
        await using var context = CreateContext();
        SeedUser(context, 7, "patient@example.test", UserRole.User);
        SeedUser(context, 8, "other@example.test", UserRole.User);
        context.AddRange(
            HealthMetric(7, 1, new DateTime(2026, 1, 1)),
            HealthMetric(7, 2, new DateTime(2026, 1, 2)),
            HealthMetric(7, 3, new DateTime(2026, 1, 3)),
            HealthMetric(8, 4, new DateTime(2026, 1, 4)));
        await context.SaveChangesAsync();

        var result = await new HealthMetricRepository(context)
            .ListByUserIdAsync(7, PageRequest.From(2, 1));

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, Assert.Single(result.Items).StressLevel);
    }

    [Fact]
    public async Task Triggers_ShouldFilterOrderAndPageInDatabase()
    {
        await using var context = CreateContext();
        SeedUser(context, 7, "patient@example.test", UserRole.User);
        SeedUser(context, 8, "other@example.test", UserRole.User);
        context.AddRange(
            Trigger(7, 1, new DateTime(2026, 1, 1)),
            Trigger(7, 2, new DateTime(2026, 1, 2)),
            Trigger(7, 3, new DateTime(2026, 1, 3)),
            Trigger(8, 4, new DateTime(2026, 1, 4)));
        await context.SaveChangesAsync();

        var result = await new StressTriggerRepository(context)
            .ListByUserIdAsync(7, PageRequest.From(2, 1));

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, Assert.Single(result.Items).StressLevel);
    }

    [Fact]
    public async Task Assessments_ShouldFilterOrderAndPageInDatabase()
    {
        await using var context = CreateContext();
        SeedUser(context, 7, "patient@example.test", UserRole.User);
        SeedUser(context, 8, "other@example.test", UserRole.User);
        context.AddRange(
            Assessment(7, 1, new DateTime(2026, 1, 1)),
            Assessment(7, 2, new DateTime(2026, 1, 2)),
            Assessment(7, 3, new DateTime(2026, 1, 3)),
            Assessment(8, 4, new DateTime(2026, 1, 4)));
        await context.SaveChangesAsync();

        var result = await new AssessmentRepository(context)
            .ListByUserIdAsync(7, PageRequest.From(2, 1));

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new DateTime(2026, 1, 2), Assert.Single(result.Items).CompletedAt);
    }

    [Fact]
    public async Task Reports_ShouldFilterOrderAndPageInDatabase()
    {
        await using var context = CreateContext();
        SeedUser(context, 7, "patient@example.test", UserRole.User);
        SeedUser(context, 8, "professional@example.test", UserRole.Psychologist);
        var professional = new Professional(8, Specialty.Clinical, "12345").WithId(20);
        context.Add(professional);
        context.AddRange(
            Appointment(7, 101).WithId(101),
            Appointment(7, 102).WithId(102),
            Appointment(7, 103).WithId(103),
            Appointment(8, 104).WithId(104));
        var reports = new[]
        {
            Report(7, 20, 101, 1, new DateTime(2026, 1, 1)),
            Report(7, 20, 102, 2, new DateTime(2026, 1, 2)),
            Report(7, 20, 103, 3, new DateTime(2026, 1, 3)),
            Report(8, 20, 104, 4, new DateTime(2026, 1, 4))
        };
        context.AddRange(reports);
        await context.SaveChangesAsync();

        var repository = new ReportRepository(context);
        var byPatient = await repository.ListByPatientIdAsync(7, PageRequest.From(2, 1));
        var byProfessional = await repository.ListByProfessionalIdAsync(20, PageRequest.From(1, 2));

        Assert.Equal(3, byPatient.TotalCount);
        Assert.Equal(2, Assert.Single(byPatient.Items).TriggersCount);
        Assert.Equal(4, byProfessional.TotalCount);
        Assert.Equal(2, byProfessional.Items.Count);
        Assert.Equal(new[] { 4, 3 }, byProfessional.Items.Select(report => report.TriggersCount));
    }

    [Fact]
    public void PaginationParameters_ShouldPreserveUnpagedModeAndEnforceMaximum()
    {
        Assert.Null(PageRequest.From(null, null));
        Assert.Equal(new PageRequest(1, 100), PageRequest.From(0, 1000));
        Assert.Equal(new PageRequest(3, 20), PageRequest.From(3, null));
    }

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"wellness-pagination-{Guid.NewGuid()}")
            .Options);

    private static void SeedUser(AppDbContext context, long id, string email, UserRole role) =>
        context.Add(new User(email, "password-hash", email, role, DateTime.UtcNow).WithId(id));

    private static HealthMetric HealthMetric(long userId, int level, DateTime createdAt) =>
        SetCreatedAt(new HealthMetric(userId, level, null, null, null), createdAt);

    private static StressTrigger Trigger(long userId, int level, DateTime triggeredAt) =>
        new(userId, $"Stress level {level}", TriggerCategory.WorkOverload, level, triggeredAt);

    private static Assessment Assessment(long userId, int answer, DateTime completedAt) =>
        new(userId, Enumerable.Repeat(answer, 10).ToArray(), completedAt);

    private static Report Report(long patientId, long professionalId, long appointmentId, int count,
        DateTime createdAt) =>
        SetCreatedAt(new Report(patientId, professionalId, appointmentId, 1, 1, 0,
            "morning", count, null, null, null, null), createdAt);

    private static Appointment Appointment(long patientId, long id) =>
        new(patientId, 20, new DateTime(2026, 2, (int)(id - 100)), EAppointmentType.TerapiaIndividual, null);

    private static T SetCreatedAt<T>(T entity, DateTime createdAt) where T : class
    {
        typeof(T).GetProperty("CreatedAt", BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, createdAt);
        return entity;
    }
}
