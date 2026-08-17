using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Drivers.DTOs;
using Shuttlez.Application.Drivers.Handlers;
using Shuttlez.Application.Drivers.Queries;
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

    [HttpGet("me/ratings")]
    public async Task<ActionResult<ApiResponse<DriverRatingsSummaryDto>>> GetMyRatings(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyDriverRatingsQuery(), cancellationToken);
        return Ok(ApiResponse<DriverRatingsSummaryDto>.Ok(result));
    }
}
