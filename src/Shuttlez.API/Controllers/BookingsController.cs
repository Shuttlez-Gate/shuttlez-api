using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Bookings.DTOs;
using Shuttlez.Application.Bookings.Queries;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("preview")]
    public async Task<ActionResult<ApiResponse<BookingPreviewDto>>> GetPreview(
        [FromQuery] double sourceLatitude,
        [FromQuery] double sourceLongitude,
        [FromQuery] double destinationLatitude,
        [FromQuery] double destinationLongitude,
        [FromQuery] string? sourceAddress,
        [FromQuery] string? destinationAddress,
        [FromQuery] string? sourceTime,
        [FromQuery] string? destinationTime,
        [FromQuery] int vehicleTypeIndex = 1,
        CancellationToken cancellationToken = default)
    {
        var preview = await _mediator.Send(
            new GetBookingPreviewQuery(
                sourceLatitude,
                sourceLongitude,
                destinationLatitude,
                destinationLongitude,
                sourceAddress,
                destinationAddress,
                sourceTime,
                destinationTime,
                vehicleTypeIndex),
            cancellationToken);

        return Ok(ApiResponse<BookingPreviewDto>.Ok(preview));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateBookingResponse>>> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateBookingCommand(request), cancellationToken);
        return Ok(ApiResponse<CreateBookingResponse>.Ok(result, result.Message));
    }
}
