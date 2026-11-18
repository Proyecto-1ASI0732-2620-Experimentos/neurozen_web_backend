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

        var title = ValidateTitle(command.Title, errors);
        var description = ValidateDescription(Clean(command.Description), errors);
        var content = ValidateContent(Clean(command.Content), errors);
        var author = ValidateAuthor(Clean(command.Author), errors);

        errors.AddIf(!ResourceTypeExtensions.TryFromWire(command.ResourceType, out var resourceType), "resourceType",
            "El tipo debe ser Video, Audio, Article o Exercise");

        var contentUrl = ValidateUrl(Clean(command.ContentUrl), "contentUrl", "La URL del contenido no es válida", errors);
        var thumbnail = ValidateUrl(Clean(command.Thumbnail), "thumbnail", "La URL de la miniatura no es válida", errors);
        ValidateDuration(command.Duration, errors);
        var tags = ValidateTags(command.Tags, errors);
        var requiredPlan = ValidatePlan(command.RequiredPlan, ResourcePlan.Advanced, errors);

        errors.ThrowIfAny();

        var resource = new ResourceLibrary(title, description, content, author, resourceType, contentUrl,
            command.Duration ?? 0, thumbnail, tags, requiredPlan);
        await repository.AddAsync(resource);
        await unitOfWork.CompleteAsync();
        return resource;
    }

    public async Task<ResourceLibrary> Handle(UpdateResourceLibraryCommand command)
    {
        var resource = await repository.FindByIdAsync(command.Id)
                       ?? throw new ResourceNotFoundException("El recurso no existe");
        var errors = new ValidationErrors();

        var title = command.Title is null ? resource.Title : ValidateTitle(command.Title, errors);
        var description = ValidateDescription(Merge(command.Description, resource.Description), errors);
        var content = ValidateContent(Merge(command.Content, resource.Content), errors);
        var author = ValidateAuthor(Merge(command.Author, resource.Author), errors);
        var contentUrl = ValidateUrl(Merge(command.ContentUrl, resource.ContentUrl), "contentUrl",
            "La URL del contenido no es válida", errors);
        var thumbnail = ValidateUrl(Merge(command.Thumbnail, resource.Thumbnail), "thumbnail",
            "La URL de la miniatura no es válida", errors);
        ValidateDuration(command.Duration, errors);
        var tags = command.Tags is null ? resource.Tags.ToList() : ValidateTags(command.Tags, errors);
        var requiredPlan = ValidatePlan(command.RequiredPlan, resource.RequiredPlan, errors);

        errors.ThrowIfAny();

        resource.Update(title, description, content, author, contentUrl, command.Duration ?? resource.Duration,
            thumbnail, tags, requiredPlan);
        await unitOfWork.CompleteAsync();
        return resource;
    }

    private static string ValidateTitle(string? value, ValidationErrors errors)
    {
        var title = (value ?? string.Empty).Trim();
        errors.AddIf(title.Length is < 3 or > ResourceLibrary.TitleMaxLength, "title",
            $"El título debe tener entre 3 y {ResourceLibrary.TitleMaxLength} caracteres");
        return title;
    }

    private static string? ValidateDescription(string? description, ValidationErrors errors)
    {
        errors.AddIf(description?.Length > ResourceLibrary.DescriptionMaxLength, "description",
            $"La descripción no puede superar {ResourceLibrary.DescriptionMaxLength} caracteres");
        return description;
    }

    private static string? ValidateContent(string? content, ValidationErrors errors)
    {
        errors.AddIf(content?.Length > ResourceLibrary.ContentMaxLength, "content",
            $"El contenido no puede superar {ResourceLibrary.ContentMaxLength} caracteres");
        return content;
    }

    private static string? ValidateAuthor(string? author, ValidationErrors errors)
    {
        errors.AddIf(author?.Length > ResourceLibrary.AuthorMaxLength, "author",
            $"El autor no puede superar {ResourceLibrary.AuthorMaxLength} caracteres");
        return author;
    }

    private static string? ValidateUrl(string? url, string field, string message, ValidationErrors errors)
    {
        errors.AddIf(url is not null && !IsHttpUrl(url), field, message);
        return url;
    }

    private static void ValidateDuration(int? duration, ValidationErrors errors) =>
        errors.AddIf(duration is < 0 or > ResourceLibrary.MaxDurationMinutes, "duration",
            $"La duración debe estar entre 0 y {ResourceLibrary.MaxDurationMinutes} minutos");

    private static List<string> ValidateTags(IReadOnlyList<string?>? values, ValidationErrors errors)
    {
        var tags = (values ?? Array.Empty<string?>())
            .Select(tag => (tag ?? string.Empty).Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        errors.AddIf(tags.Count > ResourceLibrary.MaxTags || tags.Any(tag => tag.Length > ResourceLibrary.TagMaxLength),
            "tags", $"Máximo {ResourceLibrary.MaxTags} etiquetas de hasta {ResourceLibrary.TagMaxLength} caracteres");
        return tags;
    }

    private static ResourcePlan ValidatePlan(int? value, ResourcePlan current, ValidationErrors errors)
    {
        if (value is null) return current;
        if (ResourcePlanExtensions.IsValid(value.Value)) return (ResourcePlan)value.Value;
        errors.Add("requiredPlan", "El plan debe ser 1 (Básico), 2 (Avanzado) o 3 (Profesional)");
        return current;
    }

    /// <summary>null = keep current, "" (or whitespace) = clear, otherwise the trimmed value.</summary>
    private static string? Merge(string? incoming, string? current) => incoming is null ? current : Clean(incoming);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsHttpUrl(string value) =>
        value.Length <= ResourceLibrary.UrlMaxLength &&
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
