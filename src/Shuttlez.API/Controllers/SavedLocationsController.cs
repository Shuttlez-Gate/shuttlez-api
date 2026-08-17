using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Locations.DTOs;
using Shuttlez.Application.Locations.Queries;

namespace Shuttlez.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/users/me/saved-locations")]
public class SavedLocationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SavedLocationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SavedLocationDto>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new GetSavedLocationsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SavedLocationDto>>.Ok(items));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SavedLocationDto>>> Create(
        [FromBody] CreateSavedLocationRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new CreateSavedLocationCommand(request), cancellationToken);
        return Ok(ApiResponse<SavedLocationDto>.Ok(item, "تم حفظ الموقع"));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SavedLocationDto>>> Update(
        Guid id,
        [FromBody] UpdateSavedLocationRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new UpdateSavedLocationCommand(id, request), cancellationToken);
        return Ok(ApiResponse<SavedLocationDto>.Ok(item, "تم تحديث الموقع"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSavedLocationCommand(id), cancellationToken);
        return Ok(ApiResponse.Ok("تم حذف الموقع"));
    }

    [HttpPatch("{id:guid}/favorite")]
    public async Task<ActionResult<ApiResponse<SavedLocationDto>>> ToggleFavorite(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new ToggleFavoriteLocationCommand(id), cancellationToken);
        return Ok(ApiResponse<SavedLocationDto>.Ok(item));
    }
}
