using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Wellness.Domain.Model.Queries;
using neurozen.API.Wellness.Domain.Services;
using neurozen.API.Wellness.Interfaces.REST.Resources;
using neurozen.API.Wellness.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Wellness.Interfaces.REST;

[ApiController]
[Route("api/v1/contents")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Wellness")]
public class ContentsController(IMeditationQueryService meditationQueryService) : ControllerBase
{
    [HttpGet("meditations")]
    [SwaggerOperation(Summary = "List available meditations")]
    [SwaggerResponse(StatusCodes.Status200OK, "Meditations", typeof(IEnumerable<MeditationResource>))]
    public async Task<ActionResult<IEnumerable<MeditationResource>>> GetMeditations()
    {
        var meditations = await meditationQueryService.Handle(new GetMeditationsQuery());
        return Ok(meditations.Select(WellnessAssemblers.ToResource));
    }
}
