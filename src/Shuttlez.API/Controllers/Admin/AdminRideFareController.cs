using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/ride-fare-rules")]
public class AdminRideFareController : AdminControllerBase
{
    public AdminRideFareController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<IReadOnlyList<AdminRideFareRuleDto>>>> List(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken) =>
        Send(new AdminRideFareListQuery(activeOnly), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminRideFareRuleDto>>> Create(
        [FromBody] SaveRideFareRuleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveRideFareRuleCommand(null, request), cancellationToken, "تمت الإضافة");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminRideFareRuleDto>>> Update(
        Guid id,
        [FromBody] SaveRideFareRuleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveRideFareRuleCommand(id, request), cancellationToken, "تم التحديث");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteRideFareRuleCommand(id), cancellationToken, "تم الحذف");
}
