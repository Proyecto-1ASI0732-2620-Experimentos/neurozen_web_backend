using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Appointments.Domain.Model.Commands;
using neurozen.API.Appointments.Domain.Model.Queries;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Appointments.Domain.Services;
using neurozen.API.Appointments.Interfaces.REST.Resources;
using neurozen.API.Appointments.Interfaces.REST.Transform;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Appointments.Interfaces.REST;

/// <summary>
///     Appointments. A user that is not a participant of an appointment gets 404 (its existence is not revealed).
/// </summary>
[ApiController]
[Route("api/v1/appointments")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Appointments")]
public class AppointmentsController(
    IAppointmentCommandService appointmentCommandService,
    IAppointmentQueryService appointmentQueryService) : ControllerBase
{
    [HttpGet("types")]
    [SwaggerOperation(Summary = "Appointment types (value 1-3, display name, estimated duration)")]
    [SwaggerResponse(StatusCodes.Status200OK, "Types", typeof(IEnumerable<AppointmentTypeResource>))]
    public ActionResult<IEnumerable<AppointmentTypeResource>> GetAppointmentTypes() =>
        Ok(AppointmentType.GetAll().Select(AppointmentResourceFromEntityAssembler.ToResource));

    [HttpGet]
    [SwaggerOperation(Summary = "My appointments: ?patientId= (USER, own id) or ?professionalId= (verified psychologist, own profile)",
        Description = "Ordered by date ascending. Optional page/pageSize pagination (pageSize capped at 100); " +
                      "without either parameter, returns the full list. patientName/patientEmail are included for the psychologist.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Appointments", typeof(IEnumerable<AppointmentResource>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Neither or both filters", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not your appointments", typeof(ErrorResponse))]
    public async Task<ActionResult<IEnumerable<AppointmentResource>>> GetAppointments(
        [FromQuery] long? patientId, [FromQuery] long? professionalId,
        [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var appointments = await appointmentQueryService.Handle(
            new GetAppointmentsQuery(patientId, professionalId, page, pageSize));
        if (appointments.TotalCount is { } totalCount)
            Response.Headers["X-Total-Count"] = totalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Ok(appointments.Items.Select(AppointmentResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:long}")]
    [SwaggerOperation(Summary = "Appointment detail (participants only)")]
    [SwaggerResponse(StatusCodes.Status200OK, "The appointment", typeof(AppointmentResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found or not a participant", typeof(ErrorResponse))]
    public async Task<ActionResult<AppointmentResource>> GetAppointmentById(long id)
    {
        var details = await appointmentQueryService.Handle(new GetAppointmentByIdQuery(id));
        return Ok(AppointmentResourceFromEntityAssembler.ToResourceFromEntity(details));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.User)]
    [SwaggerOperation(Summary = "Book an appointment (status PENDING)",
        Description = "patientId must be the caller. The professional must be VERIFIED, the date future and " +
                      "inside the professional's availability, and the slot free.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Booked", typeof(AppointmentResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data, past date or outside availability", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "patientId is not the caller", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Professional not found or not verified", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Slot already taken", typeof(ErrorResponse))]
    public async Task<ActionResult<AppointmentResource>> CreateAppointment(
        [FromBody] CreateAppointmentResource resource)
    {
        var details = await appointmentCommandService.Handle(
            CreateAppointmentCommandFromResourceAssembler.ToCommandFromResource(resource));
        var body = AppointmentResourceFromEntityAssembler.ToResourceFromEntity(details);
        return CreatedAtAction(nameof(GetAppointmentById), new { id = body.Id }, body);
    }

    [HttpPatch("{id:long}/status")]
    [SwaggerOperation(Summary = "Change the status: CONFIRMED / COMPLETED (psychologist) or CANCELLED (either participant)")]
    [SwaggerResponse(StatusCodes.Status200OK, "Updated", typeof(AppointmentResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid transition", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Transition not allowed for the caller", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found or not a participant", typeof(ErrorResponse))]
    public async Task<ActionResult<AppointmentResource>> UpdateStatus(long id,
        [FromBody] UpdateAppointmentStatusResource resource)
    {
        var details = await appointmentCommandService.Handle(new UpdateAppointmentStatusCommand(id, resource.Status));
        return Ok(AppointmentResourceFromEntityAssembler.ToResourceFromEntity(details));
    }
}
