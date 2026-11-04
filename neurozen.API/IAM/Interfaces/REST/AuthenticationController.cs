using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Services;
using neurozen.API.IAM.Interfaces.REST.Resources;
using neurozen.API.IAM.Interfaces.REST.Transform;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.IAM.Interfaces.REST;

[ApiController]
[AllowAnonymous]
[Route("api/v1/authentication")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Public authentication endpoints")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    IPasswordResetCommandService passwordResetCommandService) : ControllerBase
{
    [HttpPost("sign-in")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [SwaggerOperation(Summary = "Sign in with email (sent as username) and password", OperationId = "SignIn")]
    [SwaggerResponse(StatusCodes.Status200OK, "Authenticated", typeof(AuthenticatedUserResource))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Wrong credentials", typeof(ErrorResponse))]
    public async Task<ActionResult<AuthenticatedUserResource>> SignIn([FromBody] SignInResource resource)
    {
        var authenticated = await userCommandService.Handle(
            SignInCommandFromResourceAssembler.ToCommandFromResource(resource));
        return Ok(AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(authenticated));
    }

    [HttpPost("sign-up")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [SwaggerOperation(Summary = "Create a USER or PSYCHOLOGIST account", OperationId = "SignUp")]
    [SwaggerResponse(StatusCodes.Status201Created, "Created and authenticated", typeof(AuthenticatedUserResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email or license number already registered", typeof(ErrorResponse))]
    public async Task<ActionResult<AuthenticatedUserResource>> SignUp([FromBody] SignUpResource resource)
    {
        var authenticated = await userCommandService.Handle(
            SignUpCommandFromResourceAssembler.ToCommandFromResource(resource));
        var body = AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(authenticated);
        return Created($"/api/v1/users/{body.Id}", body);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
    [SwaggerOperation(Summary = "Request a reset link. Always 200, whether or not the account exists",
        OperationId = "ForgotPassword")]
    [SwaggerResponse(StatusCodes.Status200OK, "Accepted", typeof(MessageResource))]
    public async Task<ActionResult<MessageResource>> ForgotPassword([FromBody] ForgotPasswordResource resource)
    {
        await passwordResetCommandService.Handle(new ForgotPasswordCommand(resource.Email));
        return Ok(new MessageResource("ok"));
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
    [SwaggerOperation(Summary = "Set a new password with a one-time token", OperationId = "ResetPassword")]
    [SwaggerResponse(StatusCodes.Status200OK, "Password changed", typeof(MessageResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid/used/expired token or weak password",
        typeof(ErrorResponse))]
    public async Task<ActionResult<MessageResource>> ResetPassword([FromBody] ResetPasswordResource resource)
    {
        await passwordResetCommandService.Handle(new ResetPasswordCommand(resource.Token, resource.NewPassword));
        return Ok(new MessageResource("ok"));
    }
}
