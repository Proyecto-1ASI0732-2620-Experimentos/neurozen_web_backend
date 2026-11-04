using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Entities;

namespace neurozen.API.Professionals.Infrastructure.Persistence.EFC.Configuration;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    /// <summary>Referenced by the global exception handler (409 "Ya calificaste esta sesión").</summary>
    public const string AppointmentIndexName = "ux_reviews_appointment_id";

    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.ProfessionalId).IsRequired();
        builder.HasOne<Professional>().WithMany().HasForeignKey(r => r.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.AppointmentId).IsRequired();
        builder.HasOne<Appointment>().WithMany().HasForeignKey(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.AppointmentId).IsUnique().HasDatabaseName(AppointmentIndexName);

        builder.Property(r => r.PatientId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(Review.CommentMaxLength);
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => new { r.ProfessionalId, r.CreatedAt })
            .HasDatabaseName("ix_reviews_professional_id_created_at");
    }
}
