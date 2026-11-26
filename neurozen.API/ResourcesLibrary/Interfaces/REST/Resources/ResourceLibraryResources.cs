namespace neurozen.API.ResourcesLibrary.Interfaces.REST.Resources;

/// <summary>
///     List item (GET /resource-libraries). duration is in minutes. The JSON shape is frozen by
///     ResourceLibraryYouTubeLinksTests (the mobile app depends on it), so no durationMinutes alias is added here.
/// </summary>
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

/// <summary>
///     ADMIN view (GET /admin/resource-libraries[/{id}] and the POST/PATCH responses): the detail plus the plan.
///     requiredPlan: 1 = Básico, 2 = Avanzado, 3 = Profesional; requiredPlanName: "Básico", "Avanzado", "Profesional".
/// </summary>
public record AdminResourceLibraryResource(
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
    DateTime CreatedAt,
    int RequiredPlan,
    string RequiredPlanName);

/// <summary>One search hit (GET /resource-libraries/search). score: relevance between 0 and 1.</summary>
public record ResourceSearchItemResource(
    long Id,
    string Title,
    string Description,
    string ResourceType,
    string ContentUrl,
    string Thumbnail,
    int DurationMinutes,
    IReadOnlyList<string> Tags,
    double Score);

/// <summary>GET /resource-libraries/search response, most relevant first.</summary>
public record ResourceSearchResultResource(IReadOnlyList<ResourceSearchItemResource> Items);

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
