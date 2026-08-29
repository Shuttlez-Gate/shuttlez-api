using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Groups.DTOs;
using Shuttlez.Application.Groups.Handlers;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/groups")]
public class AdminGroupsController : AdminControllerBase
{
    public AdminGroupsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<GroupDto>>>> List(
        [FromQuery] string? status,
        [FromQuery] Guid? driverId,
        [FromQuery] Guid? organizerUserId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminGroupsQuery(status, driverId, organizerUserId, page, pageSize), cancellationToken);

    [HttpPut("{groupId:guid}/driver")]
    public Task<ActionResult<ApiResponse<GroupDto>>> AssignDriver(
        Guid groupId,
        [FromBody] AssignGroupDriverRequest request,
        CancellationToken cancellationToken) =>
        Send(new AssignGroupDriverCommand(groupId, request.DriverId), cancellationToken, "تم تعيين الكابتن");

    [HttpDelete("{groupId:guid}/driver")]
    public Task<ActionResult<ApiResponse<GroupDto>>> UnassignDriver(
        Guid groupId,
        CancellationToken cancellationToken) =>
        Send(new UnassignGroupDriverCommand(groupId), cancellationToken, "تم إلغاء تعيين الكابتن");
}
