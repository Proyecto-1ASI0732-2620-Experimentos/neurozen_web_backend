using System.Text.Json;
using Moq;
using neurozen.API.Tests.Unit.Support;
using neurozen.API.Wellness.Application.Internal.CommandServices;
using neurozen.API.Wellness.Domain.Entities;
using neurozen.API.Wellness.Domain.Model.Commands;
using neurozen.API.Wellness.Domain.Model.ReadModels;
using neurozen.API.Wellness.Domain.Repositories;
using neurozen.API.Wellness.Interfaces.REST.Resources;
using neurozen.API.Wellness.Interfaces.REST.Transform;

namespace neurozen.API.Tests.Unit.Wellness;

public class WellnessAssemblersTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static async Task<HealthMetric> CreateMetricAsync()
    {
        var repository = new Mock<IHealthMetricRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<HealthMetric>())).Returns(Task.CompletedTask);
        var service = new HealthMetricCommandService(repository.Object, TestEntities.PassThroughUnitOfWork().Object);
        return (await service.Handle(new CreateHealthMetricCommand(7, 5, 80, 7.5m, "descanso")))!;
    }

    [Fact]
    public async Task HealthMetric_ToResource_ShouldMapAllFields()
    {
        var metric = await CreateMetricAsync();

        var resource = WellnessAssemblers.ToResource(metric);

        Assert.Equal(metric.Id, resource.Id);
        Assert.Equal(7L, resource.UserId);
        Assert.Equal(5, resource.StressLevel);
        Assert.Equal(80, resource.HeartRate);
        Assert.Equal(7.5m, resource.SleepHours);
        Assert.Equal("descanso", resource.Notes);
        Assert.Equal(metric.CreatedAt, resource.CreatedAt);
    }

    [Fact]
    public async Task Dashboard_WithMetricAndAppointment_ShouldMapLatestValuesAndNextAppointment()
    {
        var metric = await CreateMetricAsync();
        var date = new DateTime(2026, 11, 20, 15, 0, 0, DateTimeKind.Utc);
        var details = new DashboardDetails(
            metric,
            new NextAppointmentDetails(10, 7, 3, date, 1, "PENDING", "primera sesion", false));

        var resource = WellnessAssemblers.ToResource(details);

        Assert.Equal(5, resource.StressLevel);
        Assert.Equal(80, resource.HeartRate);
        Assert.Equal(7.5m, resource.SleepHours);
        var next = Assert.IsType<NextAppointmentResource>(resource.NextAppointment);
        Assert.Equal(10L, next.Id);
        Assert.Equal(7L, next.PatientId);
        Assert.Equal(3L, next.ProfessionalId);
        Assert.Equal(date, next.AppointmentDateTime);
        Assert.Equal(1, next.AppointmentType);
        Assert.Equal("PENDING", next.Status);
        Assert.Equal("primera sesion", next.NotasAdicionales);
        Assert.False(next.Reviewed);
    }

    [Fact]
    public void Dashboard_WithoutData_ShouldReturnNulls()
    {
        var resource = WellnessAssemblers.ToResource(new DashboardDetails(null, null));

        Assert.Null(resource.StressLevel);
        Assert.Null(resource.HeartRate);
        Assert.Null(resource.SleepHours);
        Assert.Null(resource.NextAppointment);
    }

    [Fact]
    public void Meditation_ToResource_ShouldMapAllFields()
    {
        var meditation = new Meditation("Respiracion 4-7-8", "Calma rapida", 10,
            "https://example.com/img.png", "https://example.com/audio.mp3");

        var resource = WellnessAssemblers.ToResource(meditation);

        Assert.Equal(meditation.Id, resource.Id);
        Assert.Equal("Respiracion 4-7-8", resource.Title);
        Assert.Equal("Calma rapida", resource.Description);
        Assert.Equal(10, resource.DurationMinutes);
        Assert.Equal("https://example.com/img.png", resource.ImageUrl);
        Assert.Equal("https://example.com/audio.mp3", resource.AudioUrl);
    }

    [Fact]
    public void CreateHealthMetricResource_WithStressLevel_ShouldDeserialize()
    {
        var resource = JsonSerializer.Deserialize<CreateHealthMetricResource>(
            """{"stressLevel":6,"heartRate":72,"sleepHours":8,"notes":"ok"}""", WebJson);

        Assert.NotNull(resource);
        Assert.Equal(6, resource!.StressLevel);
        Assert.Equal(72, resource.HeartRate);
        Assert.Equal(8m, resource.SleepHours);
        Assert.Equal("ok", resource.Notes);
    }

    [Fact]
    public void CreateHealthMetricResource_MissingStressLevel_ShouldBeRejected()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CreateHealthMetricResource>("""{"heartRate":72}""", WebJson));
    }
}