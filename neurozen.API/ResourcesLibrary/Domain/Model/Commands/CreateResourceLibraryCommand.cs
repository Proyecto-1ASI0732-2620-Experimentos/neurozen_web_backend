namespace neurozen.API.ResourcesLibrary.Domain.Model.Commands;

public record CreateResourceLibraryCommand(
    string? Title,
    string? Description,
    string? Content,
    string? Author,
    string? ResourceType,
    string? ContentUrl,
    int? Duration,
    string? Thumbnail,
    IReadOnlyList<string?>? Tags);
