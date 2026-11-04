namespace neurozen.API.ResourcesLibrary.Interfaces.REST.Resources;

/// <summary>List item (GET /resource-libraries). duration is in minutes.</summary>
public record ResourceLibraryResource(
    long Id,
    string Title,
    string Description,
    string ResourceType,
    string ContentUrl,
    int Duration,
    string Thumbnail,
    IReadOnlyList<string> Tags,
    string Author,
    DateTime CreatedAt);

/// <summary>Detail (GET /resource-libraries/{id} and /resources/{id}): adds the plain-text content.</summary>
public record ResourceLibraryDetailResource(
    long Id,
    string Title,
    string Description,
    string Content,
    string ResourceType,
    string ContentUrl,
    int Duration,
    string Thumbnail,
    IReadOnlyList<string> Tags,
    string Author,
    DateTime CreatedAt);

public record CreateResourceLibraryResource(
    string? Title,
    string? Description,
    string? Content,
    string? Author,
    string? ResourceType,
    string? ContentUrl,
    int? Duration,
    string? Thumbnail,
    List<string?>? Tags);
