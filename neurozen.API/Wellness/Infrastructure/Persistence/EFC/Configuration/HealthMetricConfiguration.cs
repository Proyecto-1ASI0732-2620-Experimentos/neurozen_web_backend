using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Infrastructure.Persistence.EFC.Configuration;

public class HealthMetricConfiguration : IEntityTypeConfiguration<HealthMetric>
{
    public void Configure(EntityTypeBuilder<HealthMetric> builder)
    {
        builder.ToTable("health_metrics");
        builder.HasKey(metric => metric.Id);
        builder.Property(metric => metric.Id).ValueGeneratedOnAdd();
        builder.Property(metric => metric.UserId).IsRequired();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(metric => metric.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(metric => metric.StressLevel).IsRequired();
        builder.Property(metric => metric.HeartRate);
        builder.Property(metric => metric.SleepHours).HasPrecision(4, 1);
        builder.Property(metric => metric.Notes).HasMaxLength(HealthMetric.NotesMaxLength);
        builder.Property(metric => metric.CreatedAt).IsRequired();
        builder.HasIndex(metric => new { metric.UserId, metric.CreatedAt })
            .HasDatabaseName("ix_health_metrics_user_id_created_at");
    }
}
