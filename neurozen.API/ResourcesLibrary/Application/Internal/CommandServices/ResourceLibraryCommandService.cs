using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.Commands;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.ResourcesLibrary.Domain.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.ResourcesLibrary.Application.Internal.CommandServices;

/// <summary>Content management (ADMIN only; not part of the frontend contract).</summary>
public class ResourceLibraryCommandService(IResourceLibraryRepository repository, IUnitOfWork unitOfWork)
    : IResourceLibraryCommandService
{
    public async Task<ResourceLibrary> Handle(CreateResourceLibraryCommand command)
    {
        var errors = new ValidationErrors();

        var title = (command.Title ?? string.Empty).Trim();
        errors.AddIf(title.Length is < 3 or > ResourceLibrary.TitleMaxLength, "title",
            $"El título debe tener entre 3 y {ResourceLibrary.TitleMaxLength} caracteres");

        var description = Clean(command.Description);
        errors.AddIf(description?.Length > ResourceLibrary.DescriptionMaxLength, "description",
            $"La descripción no puede superar {ResourceLibrary.DescriptionMaxLength} caracteres");

        var content = Clean(command.Content);
        errors.AddIf(content?.Length > ResourceLibrary.ContentMaxLength, "content",
            $"El contenido no puede superar {ResourceLibrary.ContentMaxLength} caracteres");

        var author = Clean(command.Author);
        errors.AddIf(author?.Length > ResourceLibrary.AuthorMaxLength, "author",
            $"El autor no puede superar {ResourceLibrary.AuthorMaxLength} caracteres");

        errors.AddIf(!ResourceTypeExtensions.TryFromWire(command.ResourceType, out var resourceType), "resourceType",
            "El tipo debe ser Video, Audio, Article o Exercise");

        var contentUrl = Clean(command.ContentUrl);
        errors.AddIf(contentUrl is not null && !IsHttpUrl(contentUrl), "contentUrl", "La URL del contenido no es válida");

        var thumbnail = Clean(command.Thumbnail);
        errors.AddIf(thumbnail is not null && !IsHttpUrl(thumbnail), "thumbnail", "La URL de la miniatura no es válida");

        errors.AddIf(command.Duration is < 0 or > ResourceLibrary.MaxDurationMinutes, "duration",
            $"La duración debe estar entre 0 y {ResourceLibrary.MaxDurationMinutes} minutos");

        var tags = (command.Tags ?? Array.Empty<string?>())
            .Select(tag => (tag ?? string.Empty).Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        errors.AddIf(tags.Count > ResourceLibrary.MaxTags || tags.Any(tag => tag.Length > ResourceLibrary.TagMaxLength),
            "tags", $"Máximo {ResourceLibrary.MaxTags} etiquetas de hasta {ResourceLibrary.TagMaxLength} caracteres");

        errors.ThrowIfAny();

        var resource = new ResourceLibrary(title, description, content, author, resourceType, contentUrl,
            command.Duration ?? 0, thumbnail, tags);
        await repository.AddAsync(resource);
        await unitOfWork.CompleteAsync();
        return resource;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsHttpUrl(string value) =>
        value.Length <= ResourceLibrary.UrlMaxLength &&
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
