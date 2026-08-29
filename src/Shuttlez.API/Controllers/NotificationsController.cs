using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Notifications.Commands;
using Shuttlez.Application.Notifications.DTOs;
using Shuttlez.Application.Notifications.Queries;

namespace Shuttlez.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationGroupDto>>>> GetMine(
        CancellationToken cancellationToken)
    {
        var groups = await _mediator.Send(new GetMyNotificationsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotificationGroupDto>>.Ok(groups));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ok = await _mediator.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(ok));
    }

    /// <summary>Register / upsert FCM device token for the authenticated user.</summary>
    [HttpPost("devices")]
    public async Task<ActionResult<ApiResponse<bool>>> RegisterDevice(
        [FromBody] RegisterDeviceRequest body,
        CancellationToken cancellationToken)
    {
        var ok = await _mediator.Send(
            new RegisterDeviceCommand(body.Token, body.Platform ?? "android"),
            cancellationToken);
        return Ok(ApiResponse<bool>.Ok(ok, "تم تسجيل الجهاز"));
    }

    [HttpDelete("devices/{token}")]
    public async Task<ActionResult<ApiResponse<bool>>> UnregisterDevice(
        string token,
        CancellationToken cancellationToken)
    {
        var ok = await _mediator.Send(
            new UnregisterDeviceCommand(Uri.UnescapeDataString(token)),
            cancellationToken);
        return Ok(ApiResponse<bool>.Ok(ok));
    }
}

public record RegisterDeviceRequest(string Token, string? Platform);
