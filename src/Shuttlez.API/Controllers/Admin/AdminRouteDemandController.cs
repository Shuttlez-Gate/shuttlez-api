using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/route-demand")]
public class AdminRouteDemandController : AdminControllerBase
{
    public AdminRouteDemandController(IMediator mediator) : base(mediator) { }

    [HttpGet("summary")]
    public Task<ActionResult<ApiResponse<RouteDemandSummaryDto>>> Summary(
        CancellationToken cancellationToken) =>
        Send(new RouteDemandSummaryQuery(), cancellationToken);

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<RouteDemandRowDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? vehicleType,
        [FromQuery] string? priority,
        [FromQuery] string? status,
        [FromQuery] string? routeType,
        [FromQuery] string? routeCategory,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new RouteDemandListQuery(new RouteDemandAnalysisQuery(
            search, from, to, vehicleType, priority, status, routeType, routeCategory,
            createdFrom, createdTo, page, pageSize)), cancellationToken);

    [HttpGet("export")]
    public Task<ActionResult<ApiResponse<IReadOnlyList<RouteDemandExportRowDto>>>> Export(
        [FromQuery] string? search,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? vehicleType,
        [FromQuery] string? priority,
        [FromQuery] string? status,
        [FromQuery] string? routeType,
        [FromQuery] string? routeCategory,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        CancellationToken cancellationToken) =>
        Send(new RouteDemandExportQuery(new RouteDemandAnalysisQuery(
            search, from, to, vehicleType, priority, status, routeType, routeCategory,
            createdFrom, createdTo, null, null)), cancellationToken);

    [HttpGet("details")]
    public Task<ActionResult<ApiResponse<RouteDemandDetailsDto>>> Details(
        [FromQuery] string routeKey,
        CancellationToken cancellationToken) =>
        Send(new RouteDemandDetailsQuery(routeKey), cancellationToken);

    [HttpGet("passengers")]
    public Task<ActionResult<ApiResponse<IReadOnlyList<RouteDemandPassengerDto>>>> Passengers(
        [FromQuery] string routeKey,
        CancellationToken cancellationToken) =>
        Send(new RouteDemandPassengersQuery(routeKey), cancellationToken);

    [HttpPatch("status")]
    public Task<ActionResult<ApiResponse<RouteDemandRowDto>>> UpdateStatus(
        [FromQuery] string routeKey,
        [FromBody] UpdateRouteDemandStatusRequest request,
        CancellationToken cancellationToken) =>
        Send(new UpdateRouteDemandStatusCommand(routeKey, request), cancellationToken, "تم تحديث حالة الخط");
}
