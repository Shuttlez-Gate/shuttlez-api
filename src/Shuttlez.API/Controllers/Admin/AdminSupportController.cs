using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/support")]
public class AdminSupportController : AdminControllerBase
{
    public AdminSupportController(IMediator mediator) : base(mediator) { }

    [HttpGet("tickets")]
    public Task<ActionResult<ApiResponse<PagedResult<AdminSupportTicketDto>>>> Tickets(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminTicketsQuery(search, status, page, pageSize), cancellationToken);

    [HttpGet("tickets/{id:guid}/messages")]
    public Task<ActionResult<ApiResponse<IReadOnlyList<AdminSupportMessageDto>>>> Messages(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new AdminTicketMessagesQuery(id), cancellationToken);

    [HttpPost("tickets/{id:guid}/messages")]
    public Task<ActionResult<ApiResponse<AdminSupportMessageDto>>> Reply(
        Guid id,
        [FromBody] ReplyTicketRequest request,
        CancellationToken cancellationToken) =>
        Send(new ReplyTicketCommand(id, request), cancellationToken, "تم إرسال الرد");

    [HttpPatch("tickets/{id:guid}/status")]
    public Task<ActionResult<ApiResponse<AdminSupportTicketDto>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateTicketStatusRequest request,
        CancellationToken cancellationToken) =>
        Send(new UpdateTicketStatusCommand(id, request), cancellationToken, "تم تحديث التذكرة");
}

[Route("api/v1/admin/notifications")]
public class AdminNotificationsController : AdminControllerBase
{
    public AdminNotificationsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminNotificationDto>>>> List(
        [FromQuery] Guid? userId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminNotificationsQuery(userId, page, pageSize), cancellationToken);

    [HttpPost("broadcast")]
    public Task<ActionResult<ApiResponse<BroadcastResultDto>>> Broadcast(
        [FromBody] BroadcastNotificationRequest request,
        CancellationToken cancellationToken) =>
        Send(new BroadcastNotificationCommand(request), cancellationToken, "تم إرسال الإشعار");
}
