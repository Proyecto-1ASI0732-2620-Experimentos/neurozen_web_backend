using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.Commands;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.ResourcesLibrary.Interfaces.REST.Resources;

namespace neurozen.API.ResourcesLibrary.Interfaces.REST.Transform;

public static class ResourceLibraryAssembler
{
    public static ResourceLibraryResource ToResource(ResourceLibrary r) => new(
        r.Id, r.Title, r.Description ?? string.Empty, r.ResourceType.ToWire(), r.ContentUrl ?? string.Empty,
        r.Duration, r.Thumbnail ?? string.Empty, r.Tags, r.Author ?? string.Empty, r.CreatedAt);

    public static ResourceLibraryDetailResource ToDetailResource(ResourceLibrary r) => new(
        r.Id, r.Title, r.Description ?? string.Empty, r.Content ?? string.Empty, r.ResourceType.ToWire(),
        r.ContentUrl ?? string.Empty, r.Duration, r.Thumbnail ?? string.Empty, r.Tags, r.Author ?? string.Empty,
        r.CreatedAt);

    public static CreateResourceLibraryCommand ToCommand(CreateResourceLibraryResource r) =>
        new(r.Title, r.Description, r.Content, r.Author, r.ResourceType, r.ContentUrl, r.Duration, r.Thumbnail, r.Tags);
}
