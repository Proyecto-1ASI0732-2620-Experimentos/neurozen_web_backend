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
        ValidationFailedException e => (StatusCodes.Status400BadRequest, new ErrorResponse(e.Message, e.Errors)),
        InvalidCredentialsException e => (StatusCodes.Status401Unauthorized, new ErrorResponse(e.Message)),
        ForbiddenOperationException e => (StatusCodes.Status403Forbidden, new ErrorResponse(e.Message)),
        ResourceNotFoundException e => (StatusCodes.Status404NotFound, new ErrorResponse(e.Message)),
        ConflictException e => (StatusCodes.Status409Conflict, new ErrorResponse(e.Message)),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            new ErrorResponse("El recurso cambió mientras se procesaba la solicitud. Inténtalo de nuevo.")),
        DbUpdateException e when IsDuplicateKey(e) => (StatusCodes.Status409Conflict,
            new ErrorResponse(DuplicateKeyMessage(e))),
        BadHttpRequestException e => (e.StatusCode, new ErrorResponse("La solicitud no es válida")),
        _ => (StatusCodes.Status500InternalServerError,
            new ErrorResponse("Ocurrió un error inesperado. Inténtalo de nuevo más tarde."))
    };

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
