using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using neurozen.API.Appointments.Interfaces.ACL.Services;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Wellness.Domain.Entities;
using neurozen.API.Wellness.Application.Internal.QueryServices;
using neurozen.API.Wellness.Domain.Model.Queries;
using neurozen.API.Wellness.Infrastructure.Repositories;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Professionals;

public class ProfessionalConcurrencyTests
{
    [Fact]
    public void Professional_UpdatedAt_IsConcurrencyToken()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var property = context.Model.FindEntityType(typeof(Professional))!
            .FindProperty(nameof(Professional.UpdatedAt));

        Assert.NotNull(property);
        Assert.True(property!.IsConcurrencyToken);
    }
}

public class DashboardQueryServiceTests
{
    [Fact]
    public async Task GetDashboard_ReturnsClosestUpcomingAppointment()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        var now = DateTime.UtcNow;

        var soon = new Appointment(7, 3, now.AddDays(2), EAppointmentType.TerapiaIndividual, "Soon");
        var later = new Appointment(7, 3, now.AddDays(10), EAppointmentType.TerapiaIndividual, "Later");
        var otherUser = new Appointment(8, 3, now.AddDays(1), EAppointmentType.TerapiaIndividual, "Other");
        var past = new Appointment(7, 3, now.AddDays(-1), EAppointmentType.TerapiaIndividual, "Past");
        past.Cancel(now.AddDays(-2));
        var cancelled = new Appointment(7, 3, now.AddDays(1), EAppointmentType.TerapiaIndividual, "Cancelled");
        cancelled.Cancel(now);

        context.Set<HealthMetric>().Add(new HealthMetric(7, 4, 72, 7.2m, "Fine"));
        context.Set<Appointment>().AddRange(soon, later, otherUser, past, cancelled);
        await context.SaveChangesAsync();

        var service = new DashboardQueryService(new HealthMetricRepository(context),
            new AppointmentsContextFacade(context), new FakeCurrentUser(7, "USER"));
        var result = await service.Handle(new GetDashboardQuery(7));

        Assert.Equal(4, result.LatestMetric?.StressLevel);
        Assert.Equal(soon.Id, result.NextAppointment?.Id);
        Assert.Equal("Pending", result.NextAppointment?.Status);
    }
}
