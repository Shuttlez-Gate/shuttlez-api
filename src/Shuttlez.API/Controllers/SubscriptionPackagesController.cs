using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Subscriptions.DTOs;
using Shuttlez.Application.Subscriptions.Queries;

namespace Shuttlez.API.Controllers;

[ApiController]
[Route("api/v1/subscription-packages")]
public class SubscriptionPackagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionPackagesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubscriptionPackageDto>>>> GetPackages(
        CancellationToken cancellationToken)
    {
        var packages = await _mediator.Send(new GetSubscriptionPackagesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SubscriptionPackageDto>>.Ok(packages));
    }

    [Authorize]
    [HttpPost("{id:guid}/subscribe")]
    public async Task<ActionResult<ApiResponse<SubscribePackageResponse>>> Subscribe(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SubscribePackageCommand(id), cancellationToken);
        return Ok(ApiResponse<SubscribePackageResponse>.Ok(result));
    }
}
