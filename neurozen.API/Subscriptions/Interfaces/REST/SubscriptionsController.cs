using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using neurozen.API.Subscriptions.Domain.Model.Queries;
using neurozen.API.Subscriptions.Domain.Services;
using neurozen.API.Subscriptions.Interfaces.REST.Resources;
using neurozen.API.Subscriptions.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Subscriptions.Interfaces.REST;

/// <summary>Plan subscriptions (USER only, own subscription only). Card data handled per PCI DSS.</summary>
[ApiController]
[Route("api/v1/subscriptions")]
[Authorize(Roles = AppRoles.User)]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Subscriptions")]
public class SubscriptionsController(
    ISubscriptionCommandService commandService,
    ISubscriptionQueryService queryService) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "Subscribe to a plan (or change plan)",
        Description = "Validates Luhn, expiry and CVV; stores only the last 4 digits and the expiry. " +
                      "201 when created, 200 when the existing subscription was replaced.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Created", typeof(SubscriptionResource))]
    [SwaggerResponse(StatusCodes.Status200OK, "Replaced", typeof(SubscriptionResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "userId is not the caller", typeof(ErrorResponse))]
    public async Task<ActionResult<SubscriptionResource>> Subscribe([FromBody] CreateSubscriptionResource resource)
    {
        var result = await commandService.Handle(SubscriptionAssembler.ToCommand(resource));
        var body = SubscriptionAssembler.ToResource(result.Subscription);
        return result.Created ? StatusCode(StatusCodes.Status201Created, body) : Ok(body);
    }

    [HttpGet("user/{userId:long}")]
    [SwaggerOperation(Summary = "My subscription (404 = no plan)")]
    [SwaggerResponse(StatusCodes.Status200OK, "The subscription", typeof(SubscriptionResource))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not my id", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No subscription", typeof(ErrorResponse))]
    public async Task<ActionResult<SubscriptionResource>> GetByUserId(long userId)
    {
        var subscription = await queryService.Handle(new GetSubscriptionByUserIdQuery(userId));
        return Ok(SubscriptionAssembler.ToResource(subscription));
    }
}
