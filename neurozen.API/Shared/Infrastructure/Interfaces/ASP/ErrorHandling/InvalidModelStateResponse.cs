using Microsoft.AspNetCore.Mvc;

namespace neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;

/// <summary>
///     Replaces the default ValidationProblemDetails produced by [ApiController] (malformed JSON,
///     wrong types) with the agreed { message, errors } body using camelCase field names.
/// </summary>
public static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => ToCamelCaseField(entry.Key),
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Valor no válido"
                        : error.ErrorMessage)
                    .ToArray());

        return new BadRequestObjectResult(new ErrorResponse("Hay datos inválidos en la solicitud", errors));
    }

    private static string ToCamelCaseField(string key)
    {
        var field = key.StartsWith("$.") ? key[2..] : key.TrimStart('$');
        if (string.IsNullOrEmpty(field)) return "body";
        return char.ToLowerInvariant(field[0]) + field[1..];
    }
}
