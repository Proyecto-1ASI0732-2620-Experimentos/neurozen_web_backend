using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Subscriptions.Domain.Model.Aggregates;

namespace neurozen.API.Subscriptions.Infrastructure.Persistence.EFC.Configuration;

/// <summary>There are deliberately no columns for the full card number or the CVV.</summary>
public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public const string UserIndexName = "ux_subscriptions_user_id";

    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.UserId).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.UserId).IsUnique().HasDatabaseName(UserIndexName);

        builder.Property(s => s.PlanId).IsRequired();
        builder.Property(s => s.HolderFirstName).HasMaxLength(Subscription.NameMaxLength).IsRequired();
        builder.Property(s => s.HolderLastName).HasMaxLength(Subscription.NameMaxLength).IsRequired();
        builder.Property(s => s.Email).HasMaxLength(Subscription.EmailMaxLength).IsRequired();
        builder.Property(s => s.CardLast4).HasMaxLength(4).IsFixedLength().IsRequired();
        builder.Property(s => s.CardExpiry).HasMaxLength(5).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
