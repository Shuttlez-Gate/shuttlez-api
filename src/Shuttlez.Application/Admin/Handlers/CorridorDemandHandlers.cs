using MediatR;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Services;

namespace Shuttlez.Application.Admin.Handlers;

public record GetCorridorDemandQuery(Guid RouteId) : IRequest<CorridorDemandReportDto>;

public record ApplyCorridorDemandCommand(Guid RouteId, ApplyCorridorDemandRequest Request)
    : IRequest<ApplyCorridorDemandResultDto>;

public class CorridorDemandHandlers :
    IRequestHandler<GetCorridorDemandQuery, CorridorDemandReportDto>,
    IRequestHandler<ApplyCorridorDemandCommand, ApplyCorridorDemandResultDto>
{
    private readonly ICorridorDemandService _corridor;

    public CorridorDemandHandlers(ICorridorDemandService corridor)
    {
        _corridor = corridor;
    }

    public Task<CorridorDemandReportDto> Handle(
        GetCorridorDemandQuery request,
        CancellationToken cancellationToken) =>
        _corridor.AnalyzeAsync(request.RouteId, cancellationToken);

    public Task<ApplyCorridorDemandResultDto> Handle(
        ApplyCorridorDemandCommand request,
        CancellationToken cancellationToken) =>
        _corridor.ApplyAsync(request.RouteId, request.Request, cancellationToken);
}
