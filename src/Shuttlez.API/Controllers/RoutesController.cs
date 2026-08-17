using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Routes.DTOs;
using Shuttlez.Application.Routes.Queries;

namespace Shuttlez.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/routes")]
public class RoutesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoutesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RouteListItemDto>>>> GetRoutes(
        CancellationToken cancellationToken)
    {
        var routes = await _mediator.Send(new GetRoutesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RouteListItemDto>>.Ok(routes));
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<ApiResponse<RouteTimelineDto>>> GetTimeline(
        Guid id,
        CancellationToken cancellationToken)
    {
        var timeline = await _mediator.Send(new GetRouteTimelineQuery(id), cancellationToken);
        return Ok(ApiResponse<RouteTimelineDto>.Ok(timeline));
    }
}
