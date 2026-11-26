using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;

namespace neurozen.API.ResourcesLibrary.Domain.Model.ReadModels;

/// <summary>A resource with its relevance (0-1) for a search.</summary>
public record ResourceSearchHit(ResourceLibrary Resource, double Score);
