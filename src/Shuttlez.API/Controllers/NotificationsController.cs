using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Notifications.DTOs;
using Shuttlez.Application.Notifications.Queries;

namespace Shuttlez.API.Controllers;

[ApiController]
[Authorize]
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
}
