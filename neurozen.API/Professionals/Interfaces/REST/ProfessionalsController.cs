using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Professionals.Interfaces.REST.Resources;
using neurozen.API.Professionals.Interfaces.REST.Transform;
using neurozen.API.Resources;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Professionals.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Professionals")]
public class ProfessionalsController(
  IProfessionalCommandService professionalCommandService,
  IProfessionalQueryService professionalQueryService,
  IStringLocalizer<SharedResource> _localizer) : ControllerBase
{
  [HttpGet]
  [SwaggerOperation(
      Summary = "Get all professionals",
      Description = "Retrieves all professionals"
    )]
  [SwaggerResponse(200, "Professionals retrieved", typeof(IEnumerable<ProfessionalResource>))]
  [SwaggerResponse(500, "Internal server error")]
  public async Task<ActionResult<IEnumerable<ProfessionalResource>>> GetAllProfessionals()
  {
    String msg = _localizer.GetString("GetAllProfessionalsError");
    var getAllProfessionalsQuery = new GetAllProfessionalsQuery();
    var result = await professionalQueryService.Handle(getAllProfessionalsQuery);
    if (result == null) return Ok(Array.Empty<ProfessionalResource>());
    var professionals = result.Select(ProfessionalResourceFromEntityAssembler.ToResourceFromEntity);
    return Ok(professionals);
  }

  [HttpPost]
  [SwaggerOperation(
      Summary = "Create a new professional",
      Description = "Creates a new professional with the provided details."
    )]
  [SwaggerResponse(201, "Professional created successfully", typeof(ProfessionalResource))]
  [SwaggerResponse(400, "Professional creation failed")]
  public async Task<ActionResult> CreateProfessional([FromBody] CreateProfessionalResource resource)
  {
    string msg = _localizer.GetString("CreateProfessionalError");
    var createProfessionalCommand = CreateProfessionalCommandFromResourceAssembler.ToCommandFromResource(resource);

    var result = await professionalCommandService.Handle(createProfessionalCommand);

    if (result is null) return BadRequest(new { message = msg});
    return CreatedAtAction(nameof(GetProfessionalById), new { id = result.Id }, ProfessionalResourceFromEntityAssembler.ToResourceFromEntity(result));
  }

  [HttpGet("{id}")]
  [SwaggerOperation(
        Summary = "Get professional by ID",
        Description = "Retrieves a professional by its unique ID.")]
  [SwaggerResponse(200, "Professional retrieved successfully", typeof(ProfessionalResource))]
  [SwaggerResponse(404, "Professional not found")]
  public async Task<ActionResult> GetProfessionalById(int id)
  {
    string msg = _localizer.GetString("GetProfessionalByIdError");
    var getAppointmentByIdQuery = new GetProfessionalByIdQuery(id);
    var result = await professionalQueryService.Handle(getAppointmentByIdQuery);
    if (result is null) return BadRequest(new { message = msg });
    return Ok(ProfessionalResourceFromEntityAssembler.ToResourceFromEntity(result));
  }
}