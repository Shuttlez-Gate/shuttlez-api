using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Landing.DTOs;
using Shuttlez.Application.Landing.Handlers;
using Shuttlez.Application.Landing.Queries;
using Shuttlez.Application.RouteRequests.DTOs;

namespace Shuttlez.API.Controllers;

[ApiController]
[Route("api/v1/landing")]
public class LandingController : ControllerBase
{
    private readonly IMediator _mediator;

    public LandingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("route-request-options")]
    public async Task<ActionResult<ApiResponse<RouteRequestOptionsDto>>> GetRouteOptions(
        CancellationToken cancellationToken)
    {
        var language = Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',')[0]?.Split(';')[0]?.Trim();
        var options = await _mediator.Send(new GetLandingRouteOptionsQuery(language), cancellationToken);
        return Ok(ApiResponse<RouteRequestOptionsDto>.Ok(options));
    }

    [HttpGet("popular-routes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PopularRouteDto>>>> GetPopularRoutes(
        CancellationToken cancellationToken)
    {
        var language = Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',')[0]?.Split(';')[0]?.Trim();
        var routes = await _mediator.Send(new GetPopularRoutesQuery(language), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PopularRouteDto>>.Ok(routes));
    }

    [HttpGet("routes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LandingRouteDto>>>> GetRoutes(
        CancellationToken cancellationToken)
    {
        var language = Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',')[0]?.Split(';')[0]?.Trim();
        var routes = await _mediator.Send(new GetLandingRoutesQuery(language), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LandingRouteDto>>.Ok(routes));
    }

    [HttpGet("routes/{id:guid}/map")]
    public async Task<ActionResult<ApiResponse<LandingRouteMapDto>>> GetRouteMap(
        Guid id,
        CancellationToken cancellationToken)
    {
        var language = Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',')[0]?.Split(';')[0]?.Trim();
        var map = await _mediator.Send(new GetLandingRouteMapQuery(id, language), cancellationToken);
        return Ok(ApiResponse<LandingRouteMapDto>.Ok(map));
    }

    [HttpGet("config")]
    public async Task<ActionResult<ApiResponse<LandingPageConfigDto>>> GetConfig(
        CancellationToken cancellationToken)
    {
        var config = await _mediator.Send(new GetLandingPageConfigQuery(), cancellationToken);
        return Ok(ApiResponse<LandingPageConfigDto>.Ok(config));
    }

    [HttpPost("route-requests")]
    public async Task<ActionResult<ApiResponse<LandingSubmitResponse>>> SubmitRouteRequest(
        [FromBody] LandingRouteLeadDto request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitLandingRouteLeadCommand(request),
            cancellationToken);
        return Ok(ApiResponse<LandingSubmitResponse>.Ok(result));
    }

    [HttpPost("waitlist")]
    public async Task<ActionResult<ApiResponse<LandingSubmitResponse>>> SubmitWaitlist(
        [FromBody] LandingWaitlistDto request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitLandingWaitlistCommand(request),
            cancellationToken);
        return Ok(ApiResponse<LandingSubmitResponse>.Ok(result));
    }

    [HttpPost("captains")]
    public async Task<ActionResult<ApiResponse<LandingSubmitResponse>>> SubmitCaptain(
        [FromBody] LandingCaptainLeadDto request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitLandingCaptainLeadCommand(request),
            cancellationToken);
        return Ok(ApiResponse<LandingSubmitResponse>.Ok(result));
    }
}
