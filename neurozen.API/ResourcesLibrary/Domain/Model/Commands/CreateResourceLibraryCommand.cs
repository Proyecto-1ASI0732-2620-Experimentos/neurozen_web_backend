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
    IReadOnlyList<string?>? Tags,
    int? RequiredPlan = null);

/// <summary>
///     PATCH /resource-libraries/{id} (ADMIN). For every field: null = keep the current value; for optional fields an
///     empty string clears it. Title cannot be cleared. resourceType is not editable (a video stays a video).
///     Tags, when sent, replace the whole list. RequiredPlan: 1 = Básico, 2 = Avanzado, 3 = Profesional.
/// </summary>
public record UpdateResourceLibraryCommand(
    long Id,
    string? Title,
    string? Description,
    string? Content,
    string? Author,
    string? ContentUrl,
    int? Duration,
    string? Thumbnail,
    IReadOnlyList<string?>? Tags,
    int? RequiredPlan = null);
