using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;

/// <summary>
///     Turns exceptions into the agreed error body. Business errors keep their status
///     (400/401/403/404/409); anything unexpected becomes a generic 500 without internal details.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Unique index names used to explain duplicate-key errors (see the EF configurations).</summary>
    private static readonly (string IndexName, string Message)[] KnownUniqueIndexes =
    [
        ("ux_users_email", "Ya existe una cuenta con este correo"),
        ("ux_users_dni", "Ya existe una cuenta con este DNI"),
        ("ux_professionals_license_number", "Ya existe una cuenta con esta colegiatura"),
        ("ux_appointments_professional_active_slot", "El horario ya no está disponible"),
        ("ux_reports_appointment_id", "Ya compartiste un informe para esta cita"),
        ("ux_reviews_appointment_id", "Ya calificaste esta sesión"),
        ("ux_subscriptions_user_id", "Ya tienes una suscripción; vuelve a intentarlo")
    ];

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, response) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

        if (httpContext.Response.HasStarted) return false;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static (int Status, ErrorResponse Response) Map(Exception exception) => exception switch
    {
        ValidationFailedException e => Business(StatusCodes.Status400BadRequest, e.Message, e.Errors),
        InvalidCredentialsException e => Business(StatusCodes.Status401Unauthorized, e.Message),
        ForbiddenOperationException e => Business(StatusCodes.Status403Forbidden, e.Message),
        ResourceNotFoundException e => Business(StatusCodes.Status404NotFound, e.Message),
        ConflictException e => Business(StatusCodes.Status409Conflict, e.Message),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            new ErrorResponse("El recurso cambió mientras se procesaba la solicitud. Inténtalo de nuevo.")),
        DbUpdateException e when IsDuplicateKey(e) => (StatusCodes.Status409Conflict,
            new ErrorResponse(DuplicateKeyMessage(e))),
        BadHttpRequestException e => (e.StatusCode, new ErrorResponse(e.StatusCode == StatusCodes.Status400BadRequest
            ? "La solicitud no es válida"
            : ErrorResponse.ForStatus(e.StatusCode))),
        _ => (StatusCodes.Status500InternalServerError,
            new ErrorResponse("Ocurrió un error inesperado. Inténtalo de nuevo más tarde."))
    };

    /// <summary>
    ///     Business errors keep their own message; a missing or technical one (e.g. a .NET default text) is replaced by
    ///     a readable message for the status code, so "message" is never null, "null" or an object.
    /// </summary>
    private static (int Status, ErrorResponse Response) Business(int status, string? message,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        (status, new ErrorResponse(ErrorResponse.Readable(message, ErrorResponse.ForStatus(status)), errors));

    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry };

    private static string DuplicateKeyMessage(DbUpdateException exception)
    {
        var detail = exception.InnerException?.Message ?? string.Empty;
        foreach (var (indexName, message) in KnownUniqueIndexes)
            if (detail.Contains(indexName, StringComparison.OrdinalIgnoreCase))
                return message;
        return "El recurso ya existe";
    }
}
