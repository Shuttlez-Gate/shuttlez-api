using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/faq")]
public class AdminFaqController : AdminControllerBase
{
    public AdminFaqController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<IReadOnlyList<AdminFaqDto>>>> List(
        CancellationToken cancellationToken) =>
        Send(new AdminFaqListQuery(), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminFaqDto>>> Create(
        [FromBody] SaveFaqRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveFaqCommand(null, request), cancellationToken, "تمت الإضافة");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminFaqDto>>> Update(
        Guid id,
        [FromBody] SaveFaqRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveFaqCommand(id, request), cancellationToken, "تم التحديث");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteFaqCommand(id), cancellationToken, "تم الحذف");
}

[Route("api/v1/admin/legal")]
public class AdminLegalController : AdminControllerBase
{
    public AdminLegalController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<IReadOnlyList<AdminLegalDto>>>> List(
        CancellationToken cancellationToken) =>
        Send(new AdminLegalListQuery(), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminLegalDto>>> Create(
        [FromBody] SaveLegalRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveLegalCommand(null, request), cancellationToken, "تمت الإضافة");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminLegalDto>>> Update(
        Guid id,
        [FromBody] SaveLegalRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveLegalCommand(id, request), cancellationToken, "تم التحديث");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteLegalCommand(id), cancellationToken, "تم الحذف");
}

[Route("api/v1/admin/packages")]
public class AdminPackagesController : AdminControllerBase
{
    public AdminPackagesController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<IReadOnlyList<AdminPackageDto>>>> List(
        CancellationToken cancellationToken) =>
        Send(new AdminPackageListQuery(), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminPackageDto>>> Create(
        [FromBody] SavePackageRequest request,
        CancellationToken cancellationToken) =>
        Send(new SavePackageCommand(null, request), cancellationToken, "تمت الإضافة");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminPackageDto>>> Update(
        Guid id,
        [FromBody] SavePackageRequest request,
        CancellationToken cancellationToken) =>
        Send(new SavePackageCommand(id, request), cancellationToken, "تم التحديث");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeletePackageCommand(id), cancellationToken, "تم الحذف");
}

[Route("api/v1/admin/commission-rules")]
public class AdminCommissionController : AdminControllerBase
{
    public AdminCommissionController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<IReadOnlyList<AdminCommissionRuleDto>>>> List(
        CancellationToken cancellationToken) =>
        Send(new AdminCommissionListQuery(), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminCommissionRuleDto>>> Create(
        [FromBody] SaveCommissionRuleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveCommissionRuleCommand(null, request), cancellationToken, "تمت الإضافة");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminCommissionRuleDto>>> Update(
        Guid id,
        [FromBody] SaveCommissionRuleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveCommissionRuleCommand(id, request), cancellationToken, "تم التحديث");
}

[Route("api/v1/admin/pricing-rules")]
public class AdminPricingController : AdminControllerBase
{
    public AdminPricingController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<IReadOnlyList<AdminPricingRuleDto>>>> List(
        [FromQuery] Guid? routeId,
        [FromQuery] string? vehicleType,
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken) =>
        Send(new AdminPricingListQuery(routeId, vehicleType, activeOnly), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminPricingRuleDto>>> Create(
        [FromBody] SavePricingRuleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SavePricingRuleCommand(null, request), cancellationToken, "تمت الإضافة");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminPricingRuleDto>>> Update(
        Guid id,
        [FromBody] SavePricingRuleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SavePricingRuleCommand(id, request), cancellationToken, "تم التحديث");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeletePricingRuleCommand(id), cancellationToken, "تم الحذف");

    [HttpPost("preview")]
    public Task<ActionResult<ApiResponse<PricingPreviewDto>>> Preview(
        [FromBody] PricingPreviewRequest request,
        CancellationToken cancellationToken) =>
        Send(new PricingPreviewQuery(request), cancellationToken);
}

[Route("api/v1/admin/earnings")]
public class AdminEarningsController : AdminControllerBase
{
    public AdminEarningsController(IMediator mediator) : base(mediator) { }

    [HttpGet("trips")]
    public Task<ActionResult<ApiResponse<CaptainEarningsReportDto>>> TripEarnings(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? driverId,
        [FromQuery] Guid? routeId,
        CancellationToken cancellationToken) =>
        Send(new CaptainEarningsReportQuery(from, to, driverId, routeId), cancellationToken);
}
