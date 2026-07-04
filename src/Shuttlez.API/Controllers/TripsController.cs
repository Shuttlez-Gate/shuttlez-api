using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Trips.DTOs;
using Shuttlez.Application.Trips.Queries;

namespace Shuttlez.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/trips")]
public class TripsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TripsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<TripListResponse>>> GetMyTrips(
        CancellationToken cancellationToken)
    {
        var trips = await _mediator.Send(new GetUserTripsQuery(), cancellationToken);
        return Ok(ApiResponse<TripListResponse>.Ok(trips));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TripDetailsDto>>> GetDetails(
        Guid id,
        CancellationToken cancellationToken)
    {
        var details = await _mediator.Send(new GetTripDetailsQuery(id), cancellationToken);
        return Ok(ApiResponse<TripDetailsDto>.Ok(details));
    }

    [HttpGet("{id:guid}/invoice")]
    public async Task<ActionResult<ApiResponse<TripInvoiceDto>>> GetInvoice(
        Guid id,
        CancellationToken cancellationToken)
    {
        var invoice = await _mediator.Send(new GetTripInvoiceQuery(id), cancellationToken);
        return Ok(ApiResponse<TripInvoiceDto>.Ok(invoice));
    }
}
