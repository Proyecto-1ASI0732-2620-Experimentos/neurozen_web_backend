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

/// <summary>POST /resource-libraries (ADMIN). requiredPlan: 1 = Básico, 2 = Avanzado (default), 3 = Profesional.</summary>
public record CreateResourceLibraryResource(
    string? Title,
    string? Description,
    string? Content,
    string? Author,
    string? ResourceType,
    string? ContentUrl,
    int? Duration,
    string? Thumbnail,
    List<string?>? Tags,
    int? RequiredPlan = null);

/// <summary>
///     PATCH /resource-libraries/{id} (ADMIN): null/missing = keep; "" clears an optional field.
///     requiredPlan: 1 = Básico, 2 = Avanzado, 3 = Profesional.
/// </summary>
public record UpdateResourceLibraryResource(
    string? Title,
    string? Description,
    string? Content,
    string? Author,
    string? ContentUrl,
    int? Duration,
    string? Thumbnail,
    List<string?>? Tags,
    int? RequiredPlan = null);
