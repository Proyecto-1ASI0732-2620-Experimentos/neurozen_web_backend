using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.ValueObjects;

namespace neurozen.API.IAM.Infrastructure.Persistence.EFC.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ux_users_email");

        // Empty for anonymized (deleted) accounts, which can never sign in.
        builder.Property(u => u.PasswordHash).HasMaxLength(100).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(User.FullNameMaxLength).IsRequired();

        builder.Property(u => u.Role)
            .HasConversion(v => v.ToWire(), v => UserRoleExtensions.FromWire(v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.PhoneNumber).HasMaxLength(UserProfileRules.PhoneNumberMaxLength);
        builder.Property(u => u.Address).HasMaxLength(UserProfileRules.AddressMaxLength);
        builder.Property(u => u.AvatarUrl).HasMaxLength(UserProfileRules.AvatarUrlMaxLength);
        builder.Property(u => u.DateOfBirth);
        builder.Property(u => u.AgeRange).HasMaxLength(10);
        builder.Property(u => u.Occupation).HasMaxLength(UserProfileRules.OccupationMaxLength);
        builder.Property(u => u.WorkType).HasMaxLength(10);
        builder.Property(u => u.AcceptedTermsAt).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        builder.Property(u => u.DeletedAt);

        builder.Ignore(u => u.IsDeleted);
    }
}
