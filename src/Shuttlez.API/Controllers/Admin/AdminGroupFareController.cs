using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/group-fare-rules")]
public class AdminGroupFareController : AdminControllerBase
{
    public AdminGroupFareController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<IReadOnlyList<AdminGroupFareRuleDto>>>> List(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken) =>
        Send(new AdminGroupFareListQuery(activeOnly), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminGroupFareRuleDto>>> Create(
        [FromBody] SaveGroupFareRuleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveGroupFareRuleCommand(null, request), cancellationToken, "تمت الإضافة");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminGroupFareRuleDto>>> Update(
        Guid id,
        [FromBody] SaveGroupFareRuleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveGroupFareRuleCommand(id, request), cancellationToken, "تم التحديث");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteGroupFareRuleCommand(id), cancellationToken, "تم الحذف");
}
