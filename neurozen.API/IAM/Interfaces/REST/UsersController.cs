using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Model.Queries;
using neurozen.API.IAM.Domain.Services;
using neurozen.API.IAM.Interfaces.REST.Resources;
using neurozen.API.IAM.Interfaces.REST.Transform;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.IAM.Interfaces.REST;

/// <summary>
///     Account endpoints. Every route is owner-only: {id} must equal the "sub" of the token (403 otherwise,
///     protection against BOLA). Available to every role, including PSYCHOLOGIST accounts that are not verified.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Account endpoints (owner only)")]
public class UsersController(
    IUserQueryService userQueryService,
    IUserAccountCommandService userAccountCommandService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("{id:long}")]
    [SwaggerOperation(Summary = "Get my account", OperationId = "GetUserById")]
    [SwaggerResponse(StatusCodes.Status200OK, "The account", typeof(UserResource))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not the owner", typeof(ErrorResponse))]
    public async Task<ActionResult<UserResource>> GetUserById(long id)
    {
        EnsureOwner(id);
        var user = await userQueryService.Handle(new GetUserByIdQuery(id));
        if (user is null || user.IsDeleted) throw new ResourceNotFoundException("La cuenta no existe");
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    [HttpPut("{id:long}")]
    [SwaggerOperation(Summary = "Update my profile (and onboarding fields)", OperationId = "UpdateUser")]
    [SwaggerResponse(StatusCodes.Status200OK, "Updated account", typeof(UserResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not the owner", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email used by another account", typeof(ErrorResponse))]
    public async Task<ActionResult<UserResource>> UpdateUser(long id, [FromBody] UpdateUserProfileResource resource)
    {
        EnsureOwner(id);
        var user = await userAccountCommandService.Handle(
            UpdateUserProfileCommandFromResourceAssembler.ToCommandFromResource(id, resource));
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    [HttpPut("{id:long}/password")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [SwaggerOperation(Summary = "Change my password", OperationId = "ChangePassword")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Password changed")]
    [SwaggerResponse(StatusCodes.Status400BadRequest,
        "Wrong current password, weak password or same as current", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not the owner", typeof(ErrorResponse))]
    public async Task<IActionResult> ChangePassword(long id, [FromBody] ChangePasswordResource resource)
    {
        EnsureOwner(id);
        await userAccountCommandService.Handle(
            ChangePasswordCommandFromResourceAssembler.ToCommandFromResource(id, resource));
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [SwaggerOperation(Summary = "Delete my account and personal data (Ley 29733)", OperationId = "DeleteUser")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Account deleted")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not the owner", typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteUser(long id)
    {
        EnsureOwner(id);
        await userAccountCommandService.Handle(new DeleteAccountCommand(id));
        return NoContent();
    }

    private void EnsureOwner(long id)
    {
        if (id != currentUser.Id)
            throw new ForbiddenOperationException("No tienes permiso para acceder a esta cuenta");
    }
}
