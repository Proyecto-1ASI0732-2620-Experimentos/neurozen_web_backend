using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;

namespace neurozen.API.ResourcesLibrary.Infrastructure.Persistence.EFC.Configuration;

public class ResourceLibraryConfiguration : IEntityTypeConfiguration<ResourceLibrary>
{
    public void Configure(EntityTypeBuilder<ResourceLibrary> builder)
    {
        builder.ToTable("resource_libraries");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.Title).HasMaxLength(ResourceLibrary.TitleMaxLength).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(ResourceLibrary.DescriptionMaxLength);
        builder.Property(r => r.Content).HasColumnType("text");
        builder.Property(r => r.Author).HasMaxLength(ResourceLibrary.AuthorMaxLength);
        builder.Property(r => r.ResourceType)
            .HasConversion(v => v.ToWire(), v => ResourceTypeExtensions.FromWire(v))
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(r => r.ContentUrl).HasMaxLength(ResourceLibrary.UrlMaxLength);
        builder.Property(r => r.Duration).IsRequired();
        builder.Property(r => r.Thumbnail).HasMaxLength(ResourceLibrary.UrlMaxLength);
        builder.Property(r => r.Tags)
            .HasConversion(
                v => TagsJson.Serialize(v),
                v => TagsJson.Deserialize(v),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => TagsJson.Serialize(a) == TagsJson.Serialize(b),
                    v => TagsJson.Serialize(v).GetHashCode(),
                    v => TagsJson.Deserialize(TagsJson.Serialize(v))))
            .HasColumnType("json")
            .IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
    }
}

internal static class TagsJson
{
    public static string Serialize(IReadOnlyList<string>? value) =>
        value is null ? "[]" : JsonSerializer.Serialize(value);

    public static IReadOnlyList<string> Deserialize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : JsonSerializer.Deserialize<List<string>>(value) ?? [];
}
