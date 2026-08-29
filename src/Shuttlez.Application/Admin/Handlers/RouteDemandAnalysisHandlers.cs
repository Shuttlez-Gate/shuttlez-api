using MediatR;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Services;
using Shuttlez.Application.Common;

namespace Shuttlez.Application.Admin.Handlers;

public record RouteDemandSummaryQuery : IRequest<RouteDemandSummaryDto>;

public record RouteDemandListQuery(RouteDemandAnalysisQuery Filters)
    : IRequest<PagedResult<RouteDemandRowDto>>;

public record RouteDemandDetailsQuery(string RouteKey) : IRequest<RouteDemandDetailsDto?>;

public record RouteDemandPassengersQuery(string RouteKey)
    : IRequest<IReadOnlyList<RouteDemandPassengerDto>>;

public record RouteDemandExportQuery(RouteDemandAnalysisQuery Filters)
    : IRequest<IReadOnlyList<RouteDemandExportRowDto>>;

public record UpdateRouteDemandStatusCommand(string RouteKey, UpdateRouteDemandStatusRequest Request)
    : IRequest<RouteDemandRowDto?>;

public record MapRouteDemandCommand(string RouteKey, Guid RouteId, Guid? AdminUserId)
    : IRequest<RouteDemandRowDto?>;

public record UnmapRouteDemandCommand(string RouteKey)
    : IRequest<RouteDemandRowDto?>;

public record LaunchRouteDemandCommand(
    string RouteKey,
    LaunchRouteDemandRequest Request,
    Guid? AdminUserId) : IRequest<RouteDemandLaunchResultDto>;

public record RouteDemandLaunchPlanQuery(RouteDemandAnalysisQuery Filters)
    : IRequest<RouteLaunchPlanResponseDto>;

public record VehicleCapacitiesQuery : IRequest<IReadOnlyList<VehicleCapacityInfoDto>>;

public class RouteDemandAnalysisHandlers :
    IRequestHandler<RouteDemandSummaryQuery, RouteDemandSummaryDto>,
    IRequestHandler<RouteDemandListQuery, PagedResult<RouteDemandRowDto>>,
    IRequestHandler<RouteDemandDetailsQuery, RouteDemandDetailsDto?>,
    IRequestHandler<RouteDemandPassengersQuery, IReadOnlyList<RouteDemandPassengerDto>>,
    IRequestHandler<RouteDemandExportQuery, IReadOnlyList<RouteDemandExportRowDto>>,
    IRequestHandler<UpdateRouteDemandStatusCommand, RouteDemandRowDto?>,
    IRequestHandler<MapRouteDemandCommand, RouteDemandRowDto?>,
    IRequestHandler<UnmapRouteDemandCommand, RouteDemandRowDto?>,
    IRequestHandler<LaunchRouteDemandCommand, RouteDemandLaunchResultDto>,
    IRequestHandler<RouteDemandLaunchPlanQuery, RouteLaunchPlanResponseDto>,
    IRequestHandler<VehicleCapacitiesQuery, IReadOnlyList<VehicleCapacityInfoDto>>
{
    private readonly IRouteDemandAnalysisService _service;
    private readonly IRouteDemandLaunchService _launch;

    public RouteDemandAnalysisHandlers(
        IRouteDemandAnalysisService service,
        IRouteDemandLaunchService launch)
    {
        _service = service;
        _launch = launch;
    }

    public Task<RouteDemandSummaryDto> Handle(
        RouteDemandSummaryQuery request,
        CancellationToken cancellationToken) =>
        _service.GetSummaryAsync(cancellationToken);

    public async Task<PagedResult<RouteDemandRowDto>> Handle(
        RouteDemandListQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _service.GetRoutesAsync(request.Filters, cancellationToken);
        var page = Math.Max(1, request.Filters.Page ?? 1);
        var pageSize = Math.Clamp(request.Filters.PageSize ?? 20, 1, 100);
        return new PagedResult<RouteDemandRowDto>(items, page, pageSize, total);
    }

    public Task<RouteDemandDetailsDto?> Handle(
        RouteDemandDetailsQuery request,
        CancellationToken cancellationToken) =>
        _service.GetDetailsAsync(request.RouteKey, cancellationToken);

    public Task<IReadOnlyList<RouteDemandPassengerDto>> Handle(
        RouteDemandPassengersQuery request,
        CancellationToken cancellationToken) =>
        _service.GetPassengersAsync(request.RouteKey, cancellationToken);

    public Task<IReadOnlyList<RouteDemandExportRowDto>> Handle(
        RouteDemandExportQuery request,
        CancellationToken cancellationToken) =>
        _service.ExportAsync(request.Filters, cancellationToken);

    public Task<RouteDemandRowDto?> Handle(
        UpdateRouteDemandStatusCommand request,
        CancellationToken cancellationToken) =>
        _service.UpdateStatusAsync(request.RouteKey, request.Request, cancellationToken);

    public Task<RouteDemandRowDto?> Handle(
        MapRouteDemandCommand request,
        CancellationToken cancellationToken) =>
        _service.MapRouteAsync(request.RouteKey, request.RouteId, request.AdminUserId, cancellationToken);

    public Task<RouteDemandRowDto?> Handle(
        UnmapRouteDemandCommand request,
        CancellationToken cancellationToken) =>
        _service.UnmapRouteAsync(request.RouteKey, cancellationToken);

    public Task<RouteDemandLaunchResultDto> Handle(
        LaunchRouteDemandCommand request,
        CancellationToken cancellationToken) =>
        _launch.LaunchAsync(request.RouteKey, request.Request, request.AdminUserId, cancellationToken);

    public Task<RouteLaunchPlanResponseDto> Handle(
        RouteDemandLaunchPlanQuery request,
        CancellationToken cancellationToken) =>
        _service.GetLaunchPlanAsync(request.Filters, cancellationToken);

    public Task<IReadOnlyList<VehicleCapacityInfoDto>> Handle(
        VehicleCapacitiesQuery request,
        CancellationToken cancellationToken) =>
        _service.GetVehicleCapacitiesAsync(cancellationToken);
}
