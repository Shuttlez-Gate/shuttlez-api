using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.CustomerTrips.Commands;
using Shuttlez.Application.CustomerTrips.DTOs;

namespace Shuttlez.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/customer-trips")]
public class CustomerTripsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomerTripsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Soft-deprecated (Phase 6A). Returns CUSTOMER_TRIPS_DEPRECATED — not a Ride product.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateCustomerTripResponse>>> Create(
        [FromBody] CreateCustomerTripRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCustomerTripCommand(request), cancellationToken);
        return Ok(ApiResponse<CreateCustomerTripResponse>.Ok(result, result.Message));
    }
}
