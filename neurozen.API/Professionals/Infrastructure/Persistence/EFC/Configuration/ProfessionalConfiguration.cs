using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.ValueObjects;

namespace neurozen.API.Professionals.Infrastructure.Persistence.EFC.Configuration;

public class ProfessionalConfiguration : IEntityTypeConfiguration<Professional>
{
    public void Configure(EntityTypeBuilder<Professional> builder)
    {
        builder.ToTable("professionals");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.UserId).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique().HasDatabaseName("ux_professionals_user_id");
        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<Professional>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Specialty)
            .HasConversion(v => v.ToWire(), v => SpecialtyExtensions.FromWire(v))
            .HasMaxLength(30)
            .IsRequired();

        // Nullable only for deleted accounts; MySQL unique indexes allow several NULLs.
        builder.Property(p => p.LicenseNumber).HasMaxLength(Professional.LicenseNumberMaxLength).IsRequired(false);
        builder.HasIndex(p => p.LicenseNumber).IsUnique().HasDatabaseName("ux_professionals_license_number");

        builder.Property(p => p.VerificationStatus)
            .HasConversion(v => v.ToWire(), v => VerificationStatusExtensions.FromWire(v))
            .HasMaxLength(10)
            .IsRequired();
        builder.HasIndex(p => p.VerificationStatus).HasDatabaseName("ix_professionals_verification_status");

        builder.Property(p => p.RejectionReason).HasMaxLength(Professional.RejectionReasonMaxLength);
        builder.Property(p => p.VerifiedAt);
        builder.Property(p => p.VerifiedBy);
        builder.Property(p => p.Bio).HasMaxLength(Professional.BioMaxLength);
        builder.Property(p => p.ExperienceYears);
        builder.Property(p => p.Price).HasPrecision(7, 2);

        builder.Property(p => p.AvailabilitySchedule)
            .HasConversion(
                v => AvailabilityScheduleJson.Serialize(v),
                v => AvailabilityScheduleJson.Deserialize(v),
                new ValueComparer<AvailabilitySchedule?>(
                    (a, b) => AvailabilityScheduleJson.Serialize(a) == AvailabilityScheduleJson.Serialize(b),
                    v => AvailabilityScheduleJson.Serialize(v).GetHashCode(),
                    v => AvailabilityScheduleJson.Deserialize(AvailabilityScheduleJson.Serialize(v))))
            .HasColumnType("json");

        builder.Property(p => p.Availability).HasMaxLength(Professional.AvailabilityMaxLength);
        builder.Property(p => p.Rating).HasPrecision(2, 1).IsRequired();
        builder.Property(p => p.ReviewsCount).IsRequired();
        builder.Property(p => p.Image).HasMaxLength(Professional.ImageMaxLength);
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired().IsConcurrencyToken();
        builder.Property(p => p.DeletedAt);

        builder.Ignore(p => p.IsVerified);
        builder.Ignore(p => p.IsDeleted);
        builder.Ignore(p => p.IsVisibleToPatients);
    }
}

/// <summary>JSON (de)serialization of the availability schedule column.</summary>
internal static class AvailabilityScheduleJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(AvailabilitySchedule? value) =>
        value is null ? string.Empty : JsonSerializer.Serialize(value, Options);

    public static AvailabilitySchedule? Deserialize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Deserialize<AvailabilitySchedule>(value, Options);
}
