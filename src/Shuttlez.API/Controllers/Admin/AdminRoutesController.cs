using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/routes")]
public class AdminRoutesController : AdminControllerBase
{
    public AdminRoutesController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminRouteDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminRoutesQuery(search, isActive, page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminRouteDetailsDto>>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new AdminRouteDetailsQuery(id), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminRouteDetailsDto>>> Create(
        [FromBody] SaveRouteRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveRouteCommand(null, request), cancellationToken, "تم إنشاء الخط");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminRouteDetailsDto>>> Update(
        Guid id,
        [FromBody] SaveRouteRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveRouteCommand(id, request), cancellationToken, "تم تحديث الخط");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteRouteCommand(id), cancellationToken, "تم حذف الخط");

    [HttpPut("{id:guid}/stops")]
    public Task<ActionResult<ApiResponse<AdminRouteDetailsDto>>> ReplaceStops(
        Guid id,
        [FromBody] List<SaveStopRequest> stops,
        CancellationToken cancellationToken) =>
        Send(new ReplaceStopsCommand(id, stops), cancellationToken, "تم تحديث المحطات");

    /// <summary>تحليل طلبات المسار القريبة من polyline (±CorridorDemandMeters) وتوزيع المقاعد.</summary>
    [HttpGet("{id:guid}/demand")]
    public Task<ActionResult<ApiResponse<CorridorDemandReportDto>>> Demand(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new GetCorridorDemandQuery(id), cancellationToken);

    /// <summary>ينشئ رحلات حسب التوزيع المقترح ويحوّل الطلبات المطابقة إلى converted.</summary>
    [HttpPost("{id:guid}/demand/apply")]
    public Task<ActionResult<ApiResponse<ApplyCorridorDemandResultDto>>> ApplyDemand(
        Guid id,
        [FromBody] ApplyCorridorDemandRequest? request,
        CancellationToken cancellationToken) =>
        Send(
            new ApplyCorridorDemandCommand(id, request ?? new ApplyCorridorDemandRequest()),
            cancellationToken,
            "تم تشغيل التوزيع وإنشاء الرحلات");
}
