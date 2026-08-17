using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Queries;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/dashboard")]
public class AdminDashboardController : AdminControllerBase
{
    public AdminDashboardController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<AdminDashboardDto>>> Get(
        [FromQuery] int days,
        CancellationToken cancellationToken) =>
        Send(new AdminDashboardQuery(days <= 0 ? 30 : days), cancellationToken);
}
