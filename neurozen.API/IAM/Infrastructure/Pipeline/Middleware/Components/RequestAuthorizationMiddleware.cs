using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Queries;
using neurozen.API.IAM.Domain.Services;
using neurozen.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace neurozen.API.IAM.Infrastructure.Pipeline.Middleware.Components;

/**
 * RequestAuthorizationMiddleware is a custom middleware.
 * This middleware is used to authorize requests.
 * It validates a token is included in the request header and that the token is valid.
 * If the token is valid then it sets the user in HttpContext.Items["User"].
 */
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    /**
     * InvokeAsync is called by the ASP.NET Core runtime.
     * It is used to authorize requests.
     * It validates a token is included in the request header and that the token is valid.
     * If the token is valid then it sets the user in HttpContext.Items["User"].
     */
    public async Task InvokeAsync(
        HttpContext context,
        IUserQueryService userQueryService,
        ITokenService tokenService)
    {
        Console.WriteLine("Entering InvokeAsync");
        // If there is no endpoint (e.g. static files, probes) skip authorization
        var endpoint = context.GetEndpoint();
        // Respect built-in AllowAnonymousAttribute (and any local derived attribute)
        var allowAnonymous = endpoint == null || endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null;
        Console.WriteLine($"Allow Anonymous is {allowAnonymous}");
        if (allowAnonymous)
        {
            Console.WriteLine("Skipping authorization");
            await next(context);
            return;
        }

        Console.WriteLine("Entering authorization");

        try
        {
            // Prefer built-in ASP.NET authentication (JwtBearer). If the Authentication middleware
            // already populated HttpContext.User, use its claims to identify the user.
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var sidClaim = context.User.FindFirst(ClaimTypes.Sid) ?? context.User.FindFirst("sub") ?? context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (sidClaim != null && Guid.TryParse(sidClaim.Value, out var userId))
                {
                    var getUserByIdQuery = new GetUserByIdQuery(userId);
                    var user = await userQueryService.Handle(getUserByIdQuery);
                    if (user != null)
                    {
                        context.Items["User"] = user;
                        Console.WriteLine("User set from authenticated principal.");
                        await next(context);
                        return;
                    }
                    // fallthrough to unauthorized if user not found
                }
                // fallthrough to try tokenService validation as a fallback
            }

            // Fallback: if authentication middleware didn't populate the principal, try token service validation
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            var token = authHeader?.Split(' ').Last();
            if (string.IsNullOrWhiteSpace(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: missing token");
                return;
            }

            var validatedUserId = await tokenService.ValidateToken(token);
            if (validatedUserId == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: invalid token");
                return;
            }

            var getUserByIdQuery2 = new GetUserByIdQuery(validatedUserId.Value);
            var validatedUser = await userQueryService.Handle(getUserByIdQuery2);
            if (validatedUser == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: user not found");
                return;
            }

            context.Items["User"] = validatedUser;
            // ensure HttpContext.User is populated so Authorize attribute sees an authenticated principal
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Sid, validatedUser.Id.ToString()),
                    new Claim(ClaimTypes.Name, validatedUser.Username)
                };
                var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                context.User = new ClaimsPrincipal(identity);
            }

            Console.WriteLine("Continuing with Middleware Pipeline");
            await next(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authorization error: {ex.Message}");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
    }
}