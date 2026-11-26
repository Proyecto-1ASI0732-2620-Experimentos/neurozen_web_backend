using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Appointments.Domain.Model.Queries;
using neurozen.API.Appointments.Domain.Services;
using neurozen.API.Appointments.Interfaces.REST.Resources;
using neurozen.API.Appointments.Interfaces.REST.Transform;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Appointments.Interfaces.REST;

/// <summary>Booking calendar: free slots of a professional over a date range.</summary>
[ApiController]
[Route("api/v1/appointments/availability")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Appointments")]
public class AppointmentAvailabilityController(IAppointmentAvailabilityQueryService availabilityQueryService)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AppPolicies.VerifiedIfPsychologist)]
    [SwaggerOperation(Summary = "Free slots of a professional per date over a range (max 92 days)",
        Description = "?professionalId=&startDate=YYYY-MM-DD&endDate=YYYY-MM-DD[&durationMinutes=60]. Dates and " +
                      "slots are in America/Lima (UTC-5, no DST); appointments are stored in UTC. USER/ADMIN: any " +
                      "verified professional; PSYCHOLOGIST: only their own profile. Same rules as " +
                      "/professionals/{id}/available-slots. nextAvailableDate may be after endDate, or null.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Availability", typeof(AvailabilityResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid dates, range over 92 days or invalid duration",
        typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "A psychologist asking for another profile", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Professional not found or not verified", typeof(ErrorResponse))]
    public async Task<ActionResult<AvailabilityResource>> GetAvailability(
        [FromQuery] long? professionalId, [FromQuery] string? startDate, [FromQuery] string? endDate,
        [FromQuery] int? durationMinutes)
    {
        var calendar = await availabilityQueryService.Handle(
            new GetAppointmentAvailabilityQuery(professionalId, startDate, endDate, durationMinutes));
        return Ok(AvailabilityResourceAssembler.ToResource(calendar));
    }
}
