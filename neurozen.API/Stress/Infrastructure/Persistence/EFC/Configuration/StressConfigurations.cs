using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.ValueObjects;

namespace neurozen.API.Stress.Infrastructure.Persistence.EFC.Configuration;

public class StressTriggerConfiguration : IEntityTypeConfiguration<StressTrigger>
{
    public void Configure(EntityTypeBuilder<StressTrigger> builder)
    {
        builder.ToTable("stress_triggers");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.UserId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Description).HasMaxLength(StressTrigger.DescriptionMaxLength).IsRequired();
        builder.Property(t => t.Category)
            .HasConversion(v => v.ToWire(), v => TriggerCategoryExtensions.FromWire(v))
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(t => t.StressLevel).IsRequired();
        builder.Property(t => t.Intensity)
            .HasConversion(v => v.ToWire(), v => TriggerIntensityExtensions.FromWire(v))
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(t => t.TriggeredAt).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => new { t.UserId, t.TriggeredAt }).HasDatabaseName("ix_stress_triggers_user_id_triggered_at");
    }
}

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("assessments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.UserId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.Answers)
            .HasConversion(
                v => AnswersJson.Serialize(v),
                v => AnswersJson.Deserialize(v),
                new ValueComparer<IReadOnlyList<int>>(
                    (a, b) => AnswersJson.Serialize(a) == AnswersJson.Serialize(b),
                    v => AnswersJson.Serialize(v).GetHashCode(),
                    v => AnswersJson.Deserialize(AnswersJson.Serialize(v))))
            .HasColumnType("json")
            .IsRequired();

        builder.Property(a => a.Score).IsRequired();
        builder.Property(a => a.Level)
            .HasConversion(v => v.ToWire(), v => AssessmentLevelExtensions.FromWire(v))
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(a => a.CompletedAt).IsRequired();

        builder.HasIndex(a => new { a.UserId, a.CompletedAt }).HasDatabaseName("ix_assessments_user_id_completed_at");
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    /// <summary>Referenced by the global exception handler (409 "Ya compartiste un informe para esta cita").</summary>
    public const string AppointmentIndexName = "ux_reports_appointment_id";

    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.PatientId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.ProfessionalId).IsRequired();
        builder.HasOne<Professional>().WithMany().HasForeignKey(r => r.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.AppointmentId).IsRequired();
        builder.HasOne<Appointment>().WithMany().HasForeignKey(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.AppointmentId).IsUnique().HasDatabaseName(AppointmentIndexName);

        builder.Property(r => r.StressCurrent).HasPrecision(5, 2).IsRequired();
        builder.Property(r => r.StressAverage).HasPrecision(5, 2).IsRequired();
        builder.Property(r => r.WeeklyChange).HasPrecision(7, 2).IsRequired();
        builder.Property(r => r.PeakHours).HasMaxLength(Report.PeakHoursMaxLength);
        builder.Property(r => r.TriggersCount).IsRequired();
        builder.Property(r => r.TopCategory)
            .HasConversion(new ValueConverter<TriggerCategory, string>(
                v => v.ToWire(), v => TriggerCategoryExtensions.FromWire(v)))
            .HasMaxLength(30);
        builder.Property(r => r.LastAssessmentScore);
        builder.Property(r => r.LastAssessmentLevel)
            .HasConversion(new ValueConverter<AssessmentLevel, string>(
                v => v.ToWire(), v => AssessmentLevelExtensions.FromWire(v)))
            .HasMaxLength(10);
        builder.Property(r => r.LastAssessmentCompletedAt);
        builder.Property(r => r.Note).HasMaxLength(Report.NoteMaxLength);
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => new { r.PatientId, r.CreatedAt }).HasDatabaseName("ix_reports_patient_id_created_at");
        builder.HasIndex(r => new { r.ProfessionalId, r.CreatedAt })
            .HasDatabaseName("ix_reports_professional_id_created_at");
    }
}

/// <summary>JSON (de)serialization of the assessment answers column.</summary>
internal static class AnswersJson
{
    public static string Serialize(IReadOnlyList<int>? value) =>
        value is null ? "[]" : JsonSerializer.Serialize(value);

    public static IReadOnlyList<int> Deserialize(string value) =>
        JsonSerializer.Deserialize<List<int>>(value) ?? [];
}
