using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using neurozen.API.Resources;
using neurozen.API.ResourcesLibrary.Domain.Model.Commands;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.ResourcesLibrary.Domain.Services;
using neurozen.API.ResourcesLibrary.Interfaces.REST.Resources;
using neurozen.API.ResourcesLibrary.Interfaces.REST.Transform;

namespace neurozen.API.ResourcesLibrary.Interfaces.REST;

[ApiController]
[Route("api/v1/resource-libraries")]  // ← Ruta explícita consistente
[Produces("application/json")]
public class ResourceLibrariesController : ControllerBase
{
    private readonly IResourceLibraryCommandService _commandService;
    private readonly IResourceLibraryRepository _repository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ResourceLibrariesController(
        IResourceLibraryCommandService commandService,
        IResourceLibraryRepository repository,
        IStringLocalizer<SharedResource> localizer)
    {
        _commandService = commandService;
        _repository = repository;
        _localizer = localizer;
    }

    [HttpPost]
    public async Task<IActionResult> CreateResourceLibrary([FromBody] CreateResourceLibraryResource resource)
    {
        String msg = _localizer.GetString("CreateResourceLibraryError");
        var command = CreateResourceLibraryCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await _commandService.Handle(command);

        if (result is null)
            return BadRequest(new{ message = msg });

        var resourceLibraryResource = ResourceLibraryResourceFromEntityAssembler.ToResourceFromEntity(result);
        return CreatedAtAction(nameof(GetResourceLibraryById), new { id = result.Id }, resourceLibraryResource);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetResourceLibraryById(int id)
    {
        String msg = _localizer.GetString("GetResourceLibraryByIdError");
        var result = await _repository.FindByIdAsync(id);

        if (result is null)
            return NotFound(new { message = msg });

        var resource = ResourceLibraryResourceFromEntityAssembler.ToResourceFromEntity(result);
        return Ok(resource);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllResourceLibraries()
    {
        String msg = _localizer.GetString("GetAllResourceLibrariesError");
        var results = await _repository.ListAsync();
        if (results == null) return Ok(Array.Empty<ResourceLibraryResource>());
        var resources = results.Select(ResourceLibraryResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(resources);
    }
}
