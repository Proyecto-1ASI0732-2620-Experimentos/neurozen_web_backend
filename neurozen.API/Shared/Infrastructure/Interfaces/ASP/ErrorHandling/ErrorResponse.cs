using System.Text.Json.Serialization;

namespace neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;

/// <summary>
///     Error body agreed with the frontend: { "message": "...", "errors": { "field": ["..."] } }.
/// </summary>
public sealed record ErrorResponse(
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null);
