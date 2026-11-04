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
///     Public directory (only VERIFIED professionals exist for patients) and the psychologist portal (/me).
/// </summary>
[ApiController]
[Route("api/v1/professionals")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Professionals")]
public class ProfessionalsController(
    IProfessionalQueryService professionalQueryService,
    IProfessionalProfileCommandService professionalProfileCommandService,
    IReviewCommandService reviewCommandService,
    IReviewQueryService reviewQueryService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AppPolicies.VerifiedIfPsychologist)]
    [SwaggerOperation(Summary = "Directory of verified professionals")]
    [SwaggerResponse(StatusCodes.Status200OK, "Verified professionals", typeof(IEnumerable<ProfessionalResource>))]
    public async Task<ActionResult<IEnumerable<ProfessionalResource>>> GetAllProfessionals()
    {
        var professionals = await professionalQueryService.Handle(new GetAllProfessionalsQuery());
        return Ok(professionals.Select(ProfessionalResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = AppPolicies.VerifiedIfPsychologist)]
    [SwaggerOperation(Summary = "Verified professional by id")]
    [SwaggerResponse(StatusCodes.Status200OK, "The professional", typeof(ProfessionalResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Does not exist or is not verified", typeof(ErrorResponse))]
    public async Task<ActionResult<ProfessionalResource>> GetProfessionalById(long id)
    {
        var professional = await professionalQueryService.Handle(new GetProfessionalByIdQuery(id));
        return Ok(ProfessionalResourceFromEntityAssembler.ToResourceFromEntity(professional));
    }

    [HttpGet("{id:long}/available-slots")]
    [Authorize(Policy = AppPolicies.VerifiedIfPsychologist)]
    [SwaggerOperation(Summary = "Free 1-hour slots (Lima time, HH:mm) for a date (YYYY-MM-DD)")]
    [SwaggerResponse(StatusCodes.Status200OK, "Free slots", typeof(IEnumerable<string>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid date", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Does not exist or is not verified", typeof(ErrorResponse))]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableSlots(long id, [FromQuery] string? date)
    {
        return Ok(await professionalQueryService.Handle(new GetAvailableSlotsQuery(id, date)));
    }

    [HttpGet("{id:long}/reviews")]
    [Authorize(Policy = AppPolicies.VerifiedIfPsychologist)]
    [SwaggerOperation(Summary = "Reviews of a verified professional, most recent first")]
    [SwaggerResponse(StatusCodes.Status200OK, "Reviews", typeof(IEnumerable<ReviewResource>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Does not exist or is not verified", typeof(ErrorResponse))]
    public async Task<ActionResult<IEnumerable<ReviewResource>>> GetReviews(long id)
    {
        var reviews = await reviewQueryService.Handle(new GetProfessionalReviewsQuery(id));
        return Ok(reviews.Select(ReviewResourceAssembler.ToResource));
    }

    [HttpPost("{id:long}/reviews")]
    [Authorize(Roles = AppRoles.User)]
    [SwaggerOperation(Summary = "Rate one of my COMPLETED sessions with this professional (one review per appointment)",
        Description = "Marks appointment.reviewed = true and recomputes the professional's rating and reviews.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Created", typeof(ReviewResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data or session not completed", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Professional not found", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Already reviewed", typeof(ErrorResponse))]
    public async Task<ActionResult<ReviewResource>> CreateReview(long id, [FromBody] CreateReviewResource resource)
    {
        var review = await reviewCommandService.Handle(
            new CreateReviewCommand(id, resource.AppointmentId, resource.Rating, resource.Comment));
        return StatusCode(StatusCodes.Status201Created, ReviewResourceAssembler.ToResource(review));
    }

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Psychologist)]
    [SwaggerOperation(Summary = "My professional profile (any verification status)")]
    [SwaggerResponse(StatusCodes.Status200OK, "My profile", typeof(MyProfessionalProfileResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The account has no professional profile", typeof(ErrorResponse))]
    public async Task<ActionResult<MyProfessionalProfileResource>> GetMyProfile()
    {
        var details = await professionalQueryService.Handle(new GetMyProfessionalProfileQuery(currentUser.Id));
        return Ok(MyProfessionalProfileResourceFromEntityAssembler.ToResourceFromEntity(details));
    }

    [HttpPut("me")]
    [Authorize(Roles = AppRoles.Psychologist)]
    [SwaggerOperation(Summary = "Update my whole professional profile",
        Description = "Changing the license number or full name, or re-submitting a REJECTED request, " +
                      "sets the status back to PENDING.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Updated profile", typeof(MyProfessionalProfileResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "License number of another account", typeof(ErrorResponse))]
    public async Task<ActionResult<MyProfessionalProfileResource>> UpdateMyProfile(
        [FromBody] UpdateMyProfessionalProfileResource resource)
    {
        var details = await professionalProfileCommandService.Handle(
            UpdateMyProfessionalProfileCommandFromResourceAssembler.ToCommandFromResource(currentUser.Id, resource));
        return Ok(MyProfessionalProfileResourceFromEntityAssembler.ToResourceFromEntity(details));
    }
}
