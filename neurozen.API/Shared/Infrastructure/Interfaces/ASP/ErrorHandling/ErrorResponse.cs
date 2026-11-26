using System.Text.Json.Serialization;

namespace neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;

/// <summary>
///     Error body agreed with the frontend: { "message": "...", "errors": { "field": ["..."] } }.
///     "message" is always a readable, non-empty text: null, blank, "null", "undefined" or .NET default exception
///     texts are replaced by <see cref="DefaultMessage" />. Field messages get the same treatment.
/// </summary>
public sealed record ErrorResponse
{
    public const string DefaultMessage = "No se pudo completar la solicitud. Inténtalo de nuevo.";
    public const string DefaultFieldMessage = "Valor no válido";

    public ErrorResponse(string? message, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        Message = Readable(message, DefaultMessage);
        Errors = errors is null
            ? null
            : errors.ToDictionary(
                pair => string.IsNullOrWhiteSpace(pair.Key) ? "body" : pair.Key,
                pair => (pair.Value ?? []).Select(text => Readable(text, DefaultFieldMessage)).ToArray());
    }

    public string Message { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    /// <summary>Default message for a status code when there is no specific text (e.g. empty 404/405/415 bodies).</summary>
    public static string ForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "La solicitud no es válida",
        StatusCodes.Status401Unauthorized => "Tu sesión no es válida o venció. Inicia sesión de nuevo.",
        StatusCodes.Status403Forbidden => "No tienes permiso para realizar esta acción",
        StatusCodes.Status404NotFound => "El recurso solicitado no existe",
        StatusCodes.Status405MethodNotAllowed => "Operación no permitida",
        StatusCodes.Status409Conflict => "La solicitud entra en conflicto con el estado actual",
        StatusCodes.Status413PayloadTooLarge => "La solicitud es demasiado grande",
        StatusCodes.Status415UnsupportedMediaType => "Formato no soportado: envía JSON (Content-Type: application/json)",
        StatusCodes.Status429TooManyRequests => "Demasiados intentos. Espera unos minutos y vuelve a intentarlo.",
        >= 500 => "Ocurrió un error inesperado. Inténtalo de nuevo más tarde.",
        _ => DefaultMessage
    };

    /// <summary>True when the text can be shown to a person as-is.</summary>
    public static bool IsReadable(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        return !trimmed.Equals("null", StringComparison.OrdinalIgnoreCase) &&
               !trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase) &&
               !trimmed.StartsWith("Exception of type", StringComparison.Ordinal) &&
               !trimmed.StartsWith('{') &&
               !trimmed.StartsWith('[');
    }

    public static string Readable(string? text, string fallback) => IsReadable(text) ? text!.Trim() : fallback;
}
