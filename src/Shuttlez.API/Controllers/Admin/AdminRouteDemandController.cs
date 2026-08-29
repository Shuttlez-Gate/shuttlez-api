using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/route-demand")]
public class AdminRouteDemandController : AdminControllerBase
{
    private readonly ICurrentUserService _currentUser;

    public AdminRouteDemandController(IMediator mediator, ICurrentUserService currentUser)
        : base(mediator)
    {
        _currentUser = currentUser;
    }

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
        [FromQuery] string? launchStatus,
        [FromQuery] bool? pricingAvailable,
        [FromQuery] bool? readyToLaunch,
        CancellationToken cancellationToken) =>
        Send(new RouteDemandListQuery(new RouteDemandAnalysisQuery(
            search, from, to, vehicleType, priority, status, routeType, routeCategory,
            createdFrom, createdTo, page, pageSize, launchStatus, pricingAvailable, readyToLaunch)), cancellationToken);

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
        [FromQuery] string? launchStatus,
        [FromQuery] bool? pricingAvailable,
        [FromQuery] bool? readyToLaunch,
        CancellationToken cancellationToken) =>
        Send(new RouteDemandExportQuery(new RouteDemandAnalysisQuery(
            search, from, to, vehicleType, priority, status, routeType, routeCategory,
            createdFrom, createdTo, null, null, launchStatus, pricingAvailable, readyToLaunch)), cancellationToken);

    [HttpGet("launch-plan")]
    public Task<ActionResult<ApiResponse<RouteLaunchPlanResponseDto>>> LaunchPlan(
        [FromQuery] string? routeKey,
        [FromQuery] string? vehicleType,
        [FromQuery] string? launchStatus,
        [FromQuery] bool? readyToLaunch,
        [FromQuery] bool? pricingAvailable,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        Send(new RouteDemandLaunchPlanQuery(new RouteDemandAnalysisQuery(
            Search: search,
            VehicleType: vehicleType,
            LaunchStatus: launchStatus,
            PricingAvailable: pricingAvailable,
            ReadyToLaunch: readyToLaunch,
            RouteKey: routeKey)), cancellationToken);

    [HttpGet("vehicle-capacities")]
    public Task<ActionResult<ApiResponse<IReadOnlyList<VehicleCapacityInfoDto>>>> VehicleCapacities(
        CancellationToken cancellationToken) =>
        Send(new VehicleCapacitiesQuery(), cancellationToken);

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

    [HttpPut("map-route")]
    public Task<ActionResult<ApiResponse<RouteDemandRowDto>>> MapRoute(
        [FromQuery] string routeKey,
        [FromBody] MapRouteDemandRequest request,
        CancellationToken cancellationToken) =>
        Send(
            new MapRouteDemandCommand(routeKey, request.RouteId, _currentUser.UserId),
            cancellationToken,
            "تم ربط الطلب بالمسار الرسمي");

    [HttpDelete("map-route")]
    public Task<ActionResult<ApiResponse<RouteDemandRowDto>>> UnmapRoute(
        [FromQuery] string routeKey,
        CancellationToken cancellationToken) =>
        Send(new UnmapRouteDemandCommand(routeKey), cancellationToken, "تم إزالة ربط المسار");

    /// <summary>
    /// Admin-controlled operational launch: creates one Trip when readiness is READY.
    /// Does not convert demand to bookings. Price/capacity/commission resolved server-side.
    /// </summary>
    [HttpPost("launch")]
    public Task<ActionResult<ApiResponse<RouteDemandLaunchResultDto>>> Launch(
        [FromQuery] string routeKey,
        [FromBody] LaunchRouteDemandRequest request,
        CancellationToken cancellationToken) =>
        Send(
            new LaunchRouteDemandCommand(routeKey, request, _currentUser.UserId),
            cancellationToken,
            "تم تشغيل الخط بنجاح");
}
