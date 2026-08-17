using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/vehicles")]
public class AdminVehiclesController : AdminControllerBase
{
    public AdminVehiclesController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminVehicleDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] bool? isActive,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminVehiclesQuery(search, type, isActive, page, pageSize), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminVehicleDto>>> Create(
        [FromBody] SaveVehicleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveVehicleCommand(null, request), cancellationToken, "تمت إضافة المركبة");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminVehicleDto>>> Update(
        Guid id,
        [FromBody] SaveVehicleRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveVehicleCommand(id, request), cancellationToken, "تم تحديث المركبة");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteVehicleCommand(id), cancellationToken, "تم حذف المركبة");
}

[Route("api/v1/admin/drivers")]
public class AdminDriversController : AdminControllerBase
{
    public AdminDriversController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminDriverDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isOnline,
        [FromQuery] bool? isActive,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminDriversQuery(search, isOnline, isActive, page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminDriverDetailsDto>>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new AdminDriverDetailsQuery(id), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminDriverDto>>> Create(
        [FromBody] CreateDriverRequest request,
        CancellationToken cancellationToken) =>
        Send(new CreateDriverCommand(request), cancellationToken, "تمت إضافة الكابتن");

    [HttpPatch("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminDriverDto>>> Update(
        Guid id,
        [FromBody] UpdateDriverRequest request,
        CancellationToken cancellationToken) =>
        Send(new UpdateDriverCommand(id, request), cancellationToken, "تم تحديث الكابتن");

    [HttpPost("{id:guid}/documents")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<AdminDriverDocumentDto>>> UploadDocument(
        Guid id,
        IFormFile file,
        [FromForm] string documentType = "Other",
        [FromForm] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<AdminDriverDocumentDto>.Fail("الملف مطلوب"));
        }

        if (!Enum.TryParse<DriverDocumentType>(documentType, true, out var type))
        {
            type = DriverDocumentType.Other;
        }

        await using var stream = file.OpenReadStream();
        var result = await Mediator.Send(
            new UploadDriverDocumentCommand(
                id,
                type,
                stream,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                "admin",
                notes),
            cancellationToken);

        return Ok(ApiResponse<AdminDriverDocumentDto>.Ok(result, "تم رفع المرفق"));
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> DeleteDocument(
        Guid id,
        Guid documentId,
        CancellationToken cancellationToken) =>
        Send(new DeleteDriverDocumentCommand(id, documentId), cancellationToken, "تم حذف المرفق");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteDriverCommand(id), cancellationToken, "تم حذف الكابتن");
}
