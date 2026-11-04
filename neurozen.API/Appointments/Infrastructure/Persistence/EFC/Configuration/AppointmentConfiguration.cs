using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Aggregates;

namespace neurozen.API.Appointments.Infrastructure.Persistence.EFC.Configuration;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    /// <summary>Referenced by the global exception handler to answer 409 "El horario ya no está disponible".</summary>
    public const string ActiveSlotIndexName = "ux_appointments_professional_active_slot";

    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.PatientId).IsRequired();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.ProfessionalId).IsRequired();
        builder.HasOne<Professional>()
            .WithMany()
            .HasForeignKey(a => a.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.AppointmentDateTime).IsRequired();

        builder.Property(a => a.AppointmentType).HasConversion<int>().IsRequired();

        builder.Property(a => a.Status)
            .HasConversion(v => v.ToWire(), v => AppointmentStatusExtensions.FromWire(v))
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.NotasAdicionales).HasMaxLength(Appointment.NotesMaxLength);
        builder.Property(a => a.Reviewed).IsRequired();
        builder.Property(a => a.ActiveSlot);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired().IsConcurrencyToken();

        builder.HasIndex(a => new { a.ProfessionalId, a.ActiveSlot })
            .IsUnique()
            .HasDatabaseName(ActiveSlotIndexName);
        builder.HasIndex(a => new { a.PatientId, a.AppointmentDateTime })
            .HasDatabaseName("ix_appointments_patient_id_date");
        builder.HasIndex(a => new { a.ProfessionalId, a.AppointmentDateTime })
            .HasDatabaseName("ix_appointments_professional_id_date");

        builder.Ignore(a => a.OccupiesSlot);
    }
}
