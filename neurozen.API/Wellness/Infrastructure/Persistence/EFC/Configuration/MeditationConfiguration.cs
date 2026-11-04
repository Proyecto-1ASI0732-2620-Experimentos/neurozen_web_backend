using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Infrastructure.Persistence.EFC.Configuration;

public class MeditationConfiguration : IEntityTypeConfiguration<Meditation>
{
    public void Configure(EntityTypeBuilder<Meditation> builder)
    {
        builder.ToTable("meditations");
        builder.HasKey(meditation => meditation.Id);
        builder.Property(meditation => meditation.Id).ValueGeneratedOnAdd();
        builder.Property(meditation => meditation.Title).HasMaxLength(200).IsRequired();
        builder.Property(meditation => meditation.Description).HasMaxLength(1000).IsRequired();
        builder.Property(meditation => meditation.DurationMinutes).IsRequired();
        builder.Property(meditation => meditation.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(meditation => meditation.AudioUrl).HasMaxLength(500).IsRequired();
    }
}
