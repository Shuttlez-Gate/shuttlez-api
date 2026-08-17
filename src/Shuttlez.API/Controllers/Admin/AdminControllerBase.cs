using MediatR;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.API.Filters;
using Shuttlez.Application.Common;
using Shuttlez.Infrastructure.Configuration;

namespace Shuttlez.API.Controllers.Admin;

/// <summary>أساس كل كنترولرات لوحة التحكم: تخويل Admin + سياسة CORS الموثوقة.</summary>
[ApiController]
[AdminOnly]
[EnableCors(CorsSettings.PolicyName)]
public abstract class AdminControllerBase : ControllerBase
{
    protected AdminControllerBase(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected IMediator Mediator { get; }

    protected async Task<ActionResult<ApiResponse<T>>> Send<T>(
        IRequest<T> request,
        CancellationToken cancellationToken,
        string? message = null)
    {
        var result = await Mediator.Send(request, cancellationToken);
        return Ok(ApiResponse<T>.Ok(result, message));
    }
}
