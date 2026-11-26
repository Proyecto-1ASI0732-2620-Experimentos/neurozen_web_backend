using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;

/// <summary>
///     Replaces the default ProblemDetails ({ type, title, status } without "message") that [ApiController] writes for
///     bodiless client errors such as 415 Unsupported Media Type or a plain NotFound(), with the agreed
///     { "message": "..." } body.
/// </summary>
public sealed class ErrorResponseClientErrorFactory : IClientErrorFactory
{
    public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        var status = clientError.StatusCode ?? StatusCodes.Status400BadRequest;
        return new ObjectResult(new ErrorResponse(ErrorResponse.ForStatus(status)))
        {
            StatusCode = status,
            ContentTypes = { "application/json" }
        };
    }
}

/// <summary>
///     Last safety net for responses that leave the pipeline with an error status and no body (unknown route 404,
///     405 from routing, ...): writes { "message": "..." } so the client never has to show "null".
/// </summary>
public static class EmptyErrorBodyMiddleware
{
    public static Task WriteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        if (response.HasStarted || response.StatusCode < StatusCodes.Status400BadRequest ||
            !string.IsNullOrEmpty(response.ContentType) || response.ContentLength > 0 ||
            HttpMethods.IsHead(httpContext.Request.Method))
            return Task.CompletedTask;

        return response.WriteAsJsonAsync(new ErrorResponse(ErrorResponse.ForStatus(response.StatusCode)),
            httpContext.RequestAborted);
    }
}
