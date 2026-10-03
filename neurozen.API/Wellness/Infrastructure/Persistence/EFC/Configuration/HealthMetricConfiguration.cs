using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Infrastructure.Persistence.EFC.Configuration;

public class HealthMetricConfiguration : IEntityTypeConfiguration<HealthMetric>
{
    public void Configure(EntityTypeBuilder<HealthMetric> builder)
    {
        builder.ToTable("health_metrics");
        builder.HasKey(metric => metric.Id);
        builder.Property(metric => metric.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(metric => metric.UserId).HasColumnName("user_id").HasColumnType("char(36)").IsRequired();
        builder.Property(metric => metric.StressLevel).HasColumnName("stress_level").IsRequired();
        builder.Property(metric => metric.HeartRate).HasColumnName("heart_rate");
        builder.Property(metric => metric.SleepHours).HasColumnName("sleep_hours").HasColumnType("decimal(4,1)");
        builder.Property(metric => metric.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(metric => metric.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(metric => metric.UserId).HasDatabaseName("idx_health_metrics_user_id");
        builder.HasIndex(metric => metric.CreatedAt).HasDatabaseName("idx_health_metrics_created_at");
    }
}