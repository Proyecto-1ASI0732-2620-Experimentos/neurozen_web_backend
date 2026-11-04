using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Entities;

namespace neurozen.API.Professionals.Infrastructure.Persistence.EFC.Configuration;

public class VerificationAuditEntryConfiguration : IEntityTypeConfiguration<VerificationAuditEntry>
{
    public void Configure(EntityTypeBuilder<VerificationAuditEntry> builder)
    {
        builder.ToTable("professional_verification_audits");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.ProfessionalId).IsRequired();
        builder.HasOne<Professional>()
            .WithMany()
            .HasForeignKey(e => e.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.ProfessionalId).HasDatabaseName("ix_professional_verification_audits_professional_id");

        builder.Property(e => e.AdminUserId).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(Professional.RejectionReasonMaxLength);
        builder.Property(e => e.CreatedAt).IsRequired();
    }
}
