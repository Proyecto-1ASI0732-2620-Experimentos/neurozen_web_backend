using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Professionals.Interfaces.REST.Resources;
using neurozen.API.Professionals.Interfaces.REST.Transform;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Professionals.Interfaces.REST;

/// <summary>
///     Manual license verification (ADMIN only). The admin checks the license number in the official
///     CPsP portal (www.cpsp.pe): the name matches and the status is "HABILITADO". There is no official
///     API, so the backend does not scrape it.
/// </summary>
[ApiController]
[Route("api/v1/admin/professionals")]
[Authorize(Roles = AppRoles.Admin)]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Admin - Professionals")]
public class AdminProfessionalsController(
    IProfessionalQueryService professionalQueryService,
    IProfessionalVerificationCommandService verificationCommandService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List professionals by status (PENDING, VERIFIED, REJECTED; empty = all), oldest first")]
    [SwaggerResponse(StatusCodes.Status200OK, "Professionals", typeof(IEnumerable<AdminProfessionalResource>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid status", typeof(ErrorResponse))]
    public async Task<ActionResult<IEnumerable<AdminProfessionalResource>>> GetProfessionals(
        [FromQuery] string? status)
    {
        var professionals = await professionalQueryService.Handle(new GetProfessionalsForAdminQuery(status));
        return Ok(professionals.Select(AdminProfessionalResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPatch("{id:long}/verify")]
    [SwaggerOperation(Summary = "Approve a PENDING professional")]
    [SwaggerResponse(StatusCodes.Status200OK, "Verified", typeof(AdminProfessionalResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Not PENDING", typeof(ErrorResponse))]
    public async Task<ActionResult<AdminProfessionalResource>> Verify(long id)
    {
        var details = await verificationCommandService.Handle(new VerifyProfessionalCommand(id, currentUser.Id));
        return Ok(AdminProfessionalResourceFromEntityAssembler.ToResourceFromEntity(details));
    }

    [HttpPatch("{id:long}/reject")]
    [SwaggerOperation(Summary = "Reject a PENDING professional with a reason (10-300 characters)")]
    [SwaggerResponse(StatusCodes.Status200OK, "Rejected", typeof(AdminProfessionalResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Missing or invalid reason", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Not PENDING", typeof(ErrorResponse))]
    public async Task<ActionResult<AdminProfessionalResource>> Reject(long id,
        [FromBody] RejectProfessionalResource resource)
    {
        var details = await verificationCommandService.Handle(
            new RejectProfessionalCommand(id, currentUser.Id, resource.Reason));
        return Ok(AdminProfessionalResourceFromEntityAssembler.ToResourceFromEntity(details));
    }
}
