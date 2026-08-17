using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/users")]
public class AdminUsersController : AdminControllerBase
{
    public AdminUsersController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? userType,
        [FromQuery] bool? isActive,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminUsersQuery(search, userType, isActive, page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminUserDto>>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new AdminUserByIdQuery(id), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminUserDto>>> Create(
        [FromBody] CreateAdminUserRequest request,
        CancellationToken cancellationToken) =>
        Send(new CreateAdminUserCommand(request), cancellationToken, "تم إنشاء المستخدم");

    [HttpPatch("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminUserDto>>> Update(
        Guid id,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken) =>
        Send(new UpdateAdminUserCommand(id, request), cancellationToken, "تم تحديث المستخدم");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteAdminUserCommand(id), cancellationToken, "تم حذف المستخدم");

    [HttpPost("{id:guid}/wallet")]
    public Task<ActionResult<ApiResponse<decimal>>> AdjustWallet(
        Guid id,
        [FromBody] AdjustWalletRequest request,
        CancellationToken cancellationToken) =>
        Send(new AdjustWalletCommand(id, request), cancellationToken, "تم تعديل الرصيد");
}
