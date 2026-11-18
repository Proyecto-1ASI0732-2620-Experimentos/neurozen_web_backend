using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Entities;

namespace neurozen.API.IAM.Infrastructure.Persistence.EFC.Configuration;

public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.ToTable("user_consents");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.UserId).IsRequired();
        builder.HasIndex(c => c.UserId).HasDatabaseName("ix_user_consents_user_id");
        // Restrict: consents are evidence and must not disappear silently (users are anonymized, never deleted).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.Type)
            .HasConversion(v => v.ToWire(), v => ConsentTypeExtensions.FromWire(v))
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(c => c.Version).HasMaxLength(UserConsent.VersionMaxLength).IsRequired();
        builder.Property(c => c.AcceptedAt).IsRequired();
    }
}
