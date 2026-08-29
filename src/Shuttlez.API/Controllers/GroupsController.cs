using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Groups.DTOs;
using Shuttlez.Application.Groups.Handlers;

namespace Shuttlez.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/groups")]
public class GroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GroupsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("quote")]
    public async Task<ActionResult<ApiResponse<GroupQuoteDto>>> Quote(
        [FromQuery] string? fromZoneKey,
        [FromQuery] string? toZoneKey,
        [FromQuery] double? pickupLatitude,
        [FromQuery] double? pickupLongitude,
        [FromQuery] double? destinationLatitude,
        [FromQuery] double? destinationLongitude,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetGroupQuoteQuery(
                fromZoneKey,
                toZoneKey,
                pickupLatitude,
                pickupLongitude,
                destinationLatitude,
                destinationLongitude),
            cancellationToken);
        return Ok(ApiResponse<GroupQuoteDto>.Ok(result));
    }

    [HttpGet("fare-options")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupFareOptionDto>>>> FareOptions(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGroupFareOptionsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GroupFareOptionDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GroupDto>>> Create(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateGroupCommand(request), cancellationToken);
        return Ok(ApiResponse<GroupDto>.Ok(result, "تم إنشاء المجموعة كمسودة"));
    }

    [HttpPost("{groupId:guid}/join")]
    public async Task<ActionResult<ApiResponse<GroupDto>>> Join(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new JoinGroupCommand(groupId), cancellationToken);
        return Ok(ApiResponse<GroupDto>.Ok(result, "تم الانضمام للمجموعة"));
    }

    [HttpPost("{groupId:guid}/leave")]
    public async Task<ActionResult<ApiResponse<GroupDto>>> Leave(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LeaveGroupCommand(groupId), cancellationToken);
        return Ok(ApiResponse<GroupDto>.Ok(result, "تم مغادرة المجموعة"));
    }

    [HttpPost("{groupId:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<GroupDto>>> ConfirmCash(
        Guid groupId,
        [FromBody] ConfirmGroupCashRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ConfirmGroupCashCommand(groupId, request ?? new ConfirmGroupCashRequest()),
            cancellationToken);
        return Ok(ApiResponse<GroupDto>.Ok(result, "تم تأكيد المجموعة — الدفع نقدًا للكابتن"));
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupDto>>>> GetMe(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyGroupsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GroupDto>>.Ok(result));
    }

    [HttpGet("{groupId:guid}")]
    public async Task<ActionResult<ApiResponse<GroupDto>>> GetById(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGroupByIdQuery(groupId), cancellationToken);
        return Ok(ApiResponse<GroupDto>.Ok(result));
    }

    [HttpPost("{groupId:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<GroupDto>>> Cancel(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelGroupCommand(groupId), cancellationToken);
        return Ok(ApiResponse<GroupDto>.Ok(result, "تم إلغاء المجموعة"));
    }
}
