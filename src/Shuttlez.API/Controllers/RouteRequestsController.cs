using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.RouteRequests.Commands;
using Shuttlez.Application.RouteRequests.DTOs;

namespace Shuttlez.API.Controllers;

[ApiController]
[Route("api/v1/route-requests")]
public class RouteRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RouteRequestsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("options")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<RouteRequestOptionsDto>>> GetOptions(
        CancellationToken cancellationToken)
    {
        var language = Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',')[0]?.Split(';')[0]?.Trim();
        var options = await _mediator.Send(new GetRouteRequestOptionsQuery(language), cancellationToken);
        return Ok(ApiResponse<RouteRequestOptionsDto>.Ok(options));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CreateRouteRequestResponse>>> Create(
        [FromBody] CreateRouteRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateRouteRequestCommand(request), cancellationToken);
        return Ok(ApiResponse<CreateRouteRequestResponse>.Ok(result));
    }
}
