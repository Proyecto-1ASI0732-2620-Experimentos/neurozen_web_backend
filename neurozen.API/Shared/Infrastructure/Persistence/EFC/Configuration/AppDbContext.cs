using Microsoft.EntityFrameworkCore;
using neurozen.API.IAM.Infrastructure.Persistence.EFC.Configuration.Extensions;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

namespace neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
///     Application database context. Each bounded context applies its own configurations.
/// </summary>
public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // IAM
        builder.ApplyIamConfiguration();

        // Professionals
        builder.ApplyConfiguration(
            new Professionals.Infrastructure.Persistence.EFC.Configuration.ProfessionalConfiguration());
        builder.ApplyConfiguration(
            new Professionals.Infrastructure.Persistence.EFC.Configuration.VerificationAuditEntryConfiguration());
        builder.ApplyConfiguration(new Professionals.Infrastructure.Persistence.EFC.Configuration.ReviewConfiguration());

        // Appointments
        builder.ApplyConfiguration(
            new Appointments.Infrastructure.Persistence.EFC.Configuration.AppointmentConfiguration());

        // Stress: triggers, assessments, reports
        builder.ApplyConfiguration(new Stress.Infrastructure.Persistence.EFC.Configuration.StressTriggerConfiguration());
        builder.ApplyConfiguration(new Stress.Infrastructure.Persistence.EFC.Configuration.AssessmentConfiguration());
        builder.ApplyConfiguration(new Stress.Infrastructure.Persistence.EFC.Configuration.ReportConfiguration());

        // Resource library
        builder.ApplyConfiguration(
            new ResourcesLibrary.Infrastructure.Persistence.EFC.Configuration.ResourceLibraryConfiguration());

        // Subscriptions
        builder.ApplyConfiguration(new Subscriptions.Infrastructure.Persistence.EFC.Configuration.SubscriptionConfiguration());

        // Wellness
        builder.ApplyConfiguration(new Wellness.Infrastructure.Persistence.EFC.Configuration.MeditationConfiguration());
        builder.ApplyConfiguration(
            new Wellness.Infrastructure.Persistence.EFC.Configuration.HealthMetricConfiguration());

        // Conventions — must run after all configurations.
        builder.UseUtcDateTimes();
        builder.UseSnakeCaseNamingConvention();
    }
}
