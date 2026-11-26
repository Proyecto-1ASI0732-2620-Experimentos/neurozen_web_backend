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

    [HttpGet("resource-libraries/search")]
    [Authorize(Policy = AppPolicies.VerifiedIfPsychologist)]
    [SwaggerOperation(Summary = "Relevance search for ZenBot (respects the subscription plan)",
        Description = "?q=texto&tags=csv&type=Video|Audio|Article|Exercise&limit=1-10 (default 5). At least q or " +
                      "tags. Accent- and case-insensitive; title and tags weigh more than the description. Only " +
                      "resources included in the caller's plan. The search text is not logged.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Hits, most relevant first", typeof(ResourceSearchResultResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid parameters", typeof(ErrorResponse))]
    public async Task<ActionResult<ResourceSearchResultResource>> Search([FromQuery] string? q,
        [FromQuery] string? tags, [FromQuery] string? type, [FromQuery] int? limit)
    {
        var hits = await queryService.Handle(new SearchResourceLibrariesQuery(q, tags, type, limit));
        return Ok(new ResourceSearchResultResource(hits.Select(ResourceLibraryAssembler.ToSearchItem).ToList()));
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

    // ------------------------------------------------------------------ Content management (ADMIN only)

    [HttpGet("admin/resource-libraries")]
    [Authorize(Roles = AppRoles.Admin)]
    [SwaggerOperation(Summary = "All resources with their plan, without the plan filter (ADMIN)")]
    [SwaggerResponse(StatusCodes.Status200OK, "Resources", typeof(IEnumerable<AdminResourceLibraryResource>))]
    public async Task<ActionResult<IEnumerable<AdminResourceLibraryResource>>> GetAllForAdmin()
    {
        var resources = await queryService.Handle(new GetAllResourceLibrariesForAdminQuery());
        return Ok(resources.Select(ResourceLibraryAssembler.ToAdminResource));
    }

    [HttpGet("admin/resource-libraries/{id:long}")]
    [Authorize(Roles = AppRoles.Admin)]
    [SwaggerOperation(Summary = "One resource with its plan, to fill the edit form (ADMIN)")]
    [SwaggerResponse(StatusCodes.Status200OK, "The resource", typeof(AdminResourceLibraryResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(ErrorResponse))]
    public async Task<ActionResult<AdminResourceLibraryResource>> GetByIdForAdmin(long id)
    {
        var resource = await queryService.Handle(new GetResourceLibraryForAdminQuery(id));
        return Ok(ResourceLibraryAssembler.ToAdminResource(resource));
    }

    [HttpPost("resource-libraries")]
    [Authorize(Roles = AppRoles.Admin)]
    [SwaggerOperation(Summary = "Create a resource (ADMIN)",
        Description = "resourceType: Video | Audio | Article | Exercise. requiredPlan: 1 Básico, 2 Avanzado " +
                      "(default), 3 Profesional. Articles and exercises carry their text in content.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Created", typeof(AdminResourceLibraryResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data", typeof(ErrorResponse))]
    public async Task<ActionResult<AdminResourceLibraryResource>> Create(
        [FromBody] CreateResourceLibraryResource resource)
    {
        var created = await commandService.Handle(ResourceLibraryAssembler.ToCommand(resource));
        var body = ResourceLibraryAssembler.ToAdminResource(created);
        return CreatedAtAction(nameof(GetByIdForAdmin), new { id = body.Id }, body);
    }

    [HttpPatch("resource-libraries/{id:long}")]
    [Authorize(Roles = AppRoles.Admin)]
    [SwaggerOperation(Summary = "Edit a resource (ADMIN): link, duration, texts, thumbnail, tags or plan",
        Description = "null/missing = keep; \"\" clears an optional field. resourceType is not editable.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Updated", typeof(AdminResourceLibraryResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(ErrorResponse))]
    public async Task<ActionResult<AdminResourceLibraryResource>> Update(long id,
        [FromBody] UpdateResourceLibraryResource resource)
    {
        var updated = await commandService.Handle(ResourceLibraryAssembler.ToCommand(id, resource));
        return Ok(ResourceLibraryAssembler.ToAdminResource(updated));
    }
}
