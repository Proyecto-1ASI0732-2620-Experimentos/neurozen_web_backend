using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;

namespace neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;

/// <summary>Wellness resource (video, audio, article or exercise). Duration is always in minutes.</summary>
public class ResourceLibrary
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 1000;
    public const int ContentMaxLength = 20000;
    public const int AuthorMaxLength = 120;
    public const int UrlMaxLength = 500;
    public const int MaxDurationMinutes = 600;
    public const int MaxTags = 10;
    public const int TagMaxLength = 30;

    protected ResourceLibrary()
    {
        Title = string.Empty;
        Tags = [];
    }

    public ResourceLibrary(string title, string? description, string? content, string? author, ResourceType resourceType,
        string? contentUrl, int duration, string? thumbnail, IReadOnlyList<string> tags)
    {
        Title = title;
        Description = description;
        Content = content;
        Author = author;
        ResourceType = resourceType;
        ContentUrl = contentUrl;
        Duration = duration;
        Thumbnail = thumbnail;
        Tags = tags.ToList();
        CreatedAt = DateTime.UtcNow;
    }

    public long Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Plain text body (articles and exercises).</summary>
    public string? Content { get; private set; }

    public string? Author { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public string? ContentUrl { get; private set; }

    /// <summary>Minutes.</summary>
    public int Duration { get; private set; }

    public string? Thumbnail { get; private set; }
    public IReadOnlyList<string> Tags { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
