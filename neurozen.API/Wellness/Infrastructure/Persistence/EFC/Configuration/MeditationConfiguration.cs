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
        builder.Property(meditation => meditation.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(meditation => meditation.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(meditation => meditation.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
        builder.Property(meditation => meditation.DurationMinutes).HasColumnName("duration_minutes").IsRequired();
        builder.Property(meditation => meditation.ImageUrl).HasColumnName("image_url").HasMaxLength(500).IsRequired();
        builder.Property(meditation => meditation.AudioUrl).HasColumnName("audio_url").HasMaxLength(500).IsRequired();
    }
}