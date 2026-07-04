using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Support.DTOs;
using Shuttlez.Application.Support.Handlers;

namespace Shuttlez.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/support")]
public class SupportController : ControllerBase
{
    private readonly IMediator _mediator;

    public SupportController(IMediator mediator) => _mediator = mediator;

    [HttpGet("tickets")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupportTicketDto>>>> GetTickets(
        [FromQuery] string tab = "current",
        CancellationToken cancellationToken = default)
    {
        var tickets = await _mediator.Send(new GetSupportTicketsQuery(tab), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SupportTicketDto>>.Ok(tickets));
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<ApiResponse<CreateSupportTicketResponse>>> CreateTicket(
        [FromBody] CreateSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CreateSupportTicketCommand(request), cancellationToken);
        return Ok(ApiResponse<CreateSupportTicketResponse>.Ok(result));
    }

    [HttpGet("tickets/{id:guid}/messages")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupportMessageDto>>>> GetMessages(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var messages = await _mediator.Send(new GetSupportMessagesQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SupportMessageDto>>.Ok(messages));
    }

    [HttpPost("tickets/{id:guid}/messages")]
    public async Task<ActionResult<ApiResponse<SendSupportMessageResponse>>> SendMessage(
        Guid id,
        [FromBody] SendSupportMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new SendSupportMessageCommand(id, request),
            cancellationToken);
        return Ok(ApiResponse<SendSupportMessageResponse>.Ok(result));
    }
}
