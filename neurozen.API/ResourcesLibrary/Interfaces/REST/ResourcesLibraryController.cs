using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.ResourcesLibrary.Domain.Model.Queries;
using neurozen.API.ResourcesLibrary.Domain.Services;
using neurozen.API.ResourcesLibrary.Interfaces.REST.Resources;
using neurozen.API.ResourcesLibrary.Interfaces.REST.Transform;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.ResourcesLibrary.Interfaces.REST;

[ApiController]
[Route("api/v1")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Resource library")]
public class ResourcesLibraryController(
    IResourceLibraryQueryService queryService,
    IResourceLibraryCommandService commandService) : ControllerBase
{
    [HttpGet("resource-libraries")]
    [Authorize(Policy = AppPolicies.VerifiedIfPsychologist)]
    [SwaggerOperation(Summary = "Wellness resources (duration in minutes)")]
    [SwaggerResponse(StatusCodes.Status200OK, "Resources", typeof(IEnumerable<ResourceLibraryResource>))]
    public async Task<ActionResult<IEnumerable<ResourceLibraryResource>>> GetAll()
    {
        var resources = await queryService.Handle(new GetAllResourceLibrariesQuery());
        return Ok(resources.Select(ResourceLibraryAssembler.ToResource));
    }

    [HttpGet("resource-libraries/{id:long}")]
    [HttpGet("resources/{id:long}")]
    [Authorize(Policy = AppPolicies.VerifiedIfPsychologist)]
    [SwaggerOperation(Summary = "Resource detail with its plain-text content (alias: /resources/{id})")]
    [SwaggerResponse(StatusCodes.Status200OK, "The resource", typeof(ResourceLibraryDetailResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(ErrorResponse))]
    public async Task<ActionResult<ResourceLibraryDetailResource>> GetById(long id)
    {
        var resource = await queryService.Handle(new GetResourceLibraryByIdQuery(id));
        return Ok(ResourceLibraryAssembler.ToDetailResource(resource));
    }

    // Not part of the frontend contract: content management is restricted to ADMIN.
    [HttpPost("resource-libraries")]
    [Authorize(Roles = AppRoles.Admin)]
    [SwaggerOperation(Summary = "Create a resource (ADMIN)")]
    [SwaggerResponse(StatusCodes.Status201Created, "Created", typeof(ResourceLibraryDetailResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data", typeof(ErrorResponse))]
    public async Task<ActionResult<ResourceLibraryDetailResource>> Create(
        [FromBody] CreateResourceLibraryResource resource)
    {
        var created = await commandService.Handle(ResourceLibraryAssembler.ToCommand(resource));
        var body = ResourceLibraryAssembler.ToDetailResource(created);
        return CreatedAtAction(nameof(GetById), new { id = body.Id }, body);
    }
}
