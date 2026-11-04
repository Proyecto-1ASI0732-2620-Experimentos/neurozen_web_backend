namespace neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;

/// <summary>Wire value is the name as-is: "Video", "Audio", "Article", "Exercise".</summary>
public enum ResourceType
{
    Video,
    Audio,
    Article,
    Exercise
}

public static class ResourceTypeExtensions
{
    public static string ToWire(this ResourceType type) => type.ToString();

    public static ResourceType FromWire(string value) => Enum.Parse<ResourceType>(value, true);

    public static bool TryFromWire(string? value, out ResourceType type) =>
        Enum.TryParse(value?.Trim(), true, out type) && Enum.IsDefined(type) && !int.TryParse(value, out _);
}
