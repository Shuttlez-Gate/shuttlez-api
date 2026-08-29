using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Rides.DTOs;
using Shuttlez.Application.Rides.Handlers;

namespace Shuttlez.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rides")]
public class RidesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RidesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("quote")]
    public async Task<ActionResult<ApiResponse<RideQuoteDto>>> Quote(
        [FromQuery] string? fromZoneKey,
        [FromQuery] string? toZoneKey,
        [FromQuery] double? pickupLatitude,
        [FromQuery] double? pickupLongitude,
        [FromQuery] double? destinationLatitude,
        [FromQuery] double? destinationLongitude,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetRideQuoteQuery(
                fromZoneKey,
                toZoneKey,
                pickupLatitude,
                pickupLongitude,
                destinationLatitude,
                destinationLongitude),
            cancellationToken);
        return Ok(ApiResponse<RideQuoteDto>.Ok(result));
    }

    [HttpGet("fare-options")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RideFareOptionDto>>>> FareOptions(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRideFareOptionsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RideFareOptionDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RideDto>>> Create(
        [FromBody] CreateRideRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateRideCommand(request), cancellationToken);
        return Ok(ApiResponse<RideDto>.Ok(result, "تم إنشاء طلب المشوار — الدفع نقدًا للكابتن"));
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RideDto>>>> GetMe(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyRidesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RideDto>>.Ok(result));
    }

    [HttpGet("{rideId:guid}")]
    public async Task<ActionResult<ApiResponse<RideDto>>> GetById(
        Guid rideId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRideByIdQuery(rideId), cancellationToken);
        return Ok(ApiResponse<RideDto>.Ok(result));
    }

    [HttpPost("{rideId:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<RideDto>>> Cancel(
        Guid rideId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelRideCommand(rideId), cancellationToken);
        return Ok(ApiResponse<RideDto>.Ok(result, "تم إلغاء المشوار"));
    }
}
