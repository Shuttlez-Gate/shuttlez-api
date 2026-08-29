using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Drivers.Commands;
using Shuttlez.Application.Drivers.DTOs;
using Shuttlez.Application.Drivers.Handlers;
using Shuttlez.Application.Drivers.Queries;
using Shuttlez.Application.Groups.DTOs;
using Shuttlez.Application.Groups.Handlers;
using Shuttlez.Application.Rides.DTOs;
using Shuttlez.Application.Rides.Handlers;
using Shuttlez.Domain.Enums;

namespace Shuttlez.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/drivers")]
public class DriversController : ControllerBase
{
    private readonly IMediator _mediator;

    public DriversController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<DriverProfileDto>>> GetMe(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyDriverProfileQuery(), cancellationToken);
        return Ok(ApiResponse<DriverProfileDto>.Ok(result));
    }

    [HttpPost("me/documents")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<AdminDriverDocumentDto>>> UploadMyDocument(
        IFormFile file,
        [FromForm] string documentType = "Other",
        [FromForm] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<AdminDriverDocumentDto>.Fail("الملف مطلوب"));
        }

        if (!Enum.TryParse<DriverDocumentType>(documentType, true, out var type))
        {
            type = DriverDocumentType.Other;
        }

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(
            new UploadMyDriverDocumentCommand(
                type,
                stream,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                notes),
            cancellationToken);

        return Ok(ApiResponse<AdminDriverDocumentDto>.Ok(result, "تم رفع المرفق"));
    }

    [HttpGet("me/trips")]
    public async Task<ActionResult<ApiResponse<DriverTripListResponseDto>>> GetMyTrips(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyDriverTripsQuery(), cancellationToken);
        return Ok(ApiResponse<DriverTripListResponseDto>.Ok(result));
    }

    /// <summary>Captain starts an assigned trip. No body — ownership from JWT.</summary>
    [HttpPost("me/trips/{tripId:guid}/start")]
    public async Task<ActionResult<ApiResponse<DriverTripLifecycleDto>>> StartMyTrip(
        Guid tripId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartMyDriverTripCommand(tripId), cancellationToken);
        return Ok(ApiResponse<DriverTripLifecycleDto>.Ok(result, result.Message));
    }

    /// <summary>Captain completes an in-progress trip. No body — ownership from JWT.</summary>
    [HttpPost("me/trips/{tripId:guid}/complete")]
    public async Task<ActionResult<ApiResponse<DriverTripLifecycleDto>>> CompleteMyTrip(
        Guid tripId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteMyDriverTripCommand(tripId), cancellationToken);
        return Ok(ApiResponse<DriverTripLifecycleDto>.Ok(result, result.Message));
    }

    [HttpGet("me/ratings")]
    public async Task<ActionResult<ApiResponse<DriverRatingsSummaryDto>>> GetMyRatings(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyDriverRatingsQuery(), cancellationToken);
        return Ok(ApiResponse<DriverRatingsSummaryDto>.Ok(result));
    }

    [HttpGet("me/rides")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RideDto>>>> GetMyRides(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyDriverRidesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RideDto>>.Ok(result));
    }

    [HttpPost("me/rides/{rideId:guid}/start")]
    public async Task<ActionResult<ApiResponse<DriverRideLifecycleDto>>> StartMyRide(
        Guid rideId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartMyDriverRideCommand(rideId), cancellationToken);
        return Ok(ApiResponse<DriverRideLifecycleDto>.Ok(result, result.Message));
    }

    [HttpPost("me/rides/{rideId:guid}/complete")]
    public async Task<ActionResult<ApiResponse<DriverRideLifecycleDto>>> CompleteMyRide(
        Guid rideId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteMyDriverRideCommand(rideId), cancellationToken);
        return Ok(ApiResponse<DriverRideLifecycleDto>.Ok(result, result.Message));
    }

    /// <summary>Captain updates own GPS for an Assigned/InProgress ride.</summary>
    [HttpPost("me/rides/{rideId:guid}/location")]
    public async Task<ActionResult<ApiResponse<DriverRideLocationDto>>> UpdateMyRideLocation(
        Guid rideId,
        [FromBody] UpdateCaptainRideLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateMyDriverRideLocationCommand(rideId, request.Latitude, request.Longitude),
            cancellationToken);
        return Ok(ApiResponse<DriverRideLocationDto>.Ok(result));
    }

    [HttpGet("me/groups")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupDto>>>> GetMyGroups(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyDriverGroupsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GroupDto>>.Ok(result));
    }

    [HttpPost("me/groups/{groupId:guid}/start")]
    public async Task<ActionResult<ApiResponse<DriverGroupLifecycleDto>>> StartMyGroup(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartMyDriverGroupCommand(groupId), cancellationToken);
        return Ok(ApiResponse<DriverGroupLifecycleDto>.Ok(result, result.Message));
    }

    [HttpPost("me/groups/{groupId:guid}/complete")]
    public async Task<ActionResult<ApiResponse<DriverGroupLifecycleDto>>> CompleteMyGroup(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteMyDriverGroupCommand(groupId), cancellationToken);
        return Ok(ApiResponse<DriverGroupLifecycleDto>.Ok(result, result.Message));
    }
}
