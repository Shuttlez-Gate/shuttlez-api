using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Rides.DTOs;
using Shuttlez.Application.Rides.Handlers;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/rides")]
public class AdminRidesController : AdminControllerBase
{
    public AdminRidesController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<RideDto>>>> List(
        [FromQuery] string? status,
        [FromQuery] Guid? driverId,
        [FromQuery] Guid? riderUserId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminRidesQuery(status, driverId, riderUserId, page, pageSize), cancellationToken);

    [HttpPut("{rideId:guid}/driver")]
    public Task<ActionResult<ApiResponse<RideDto>>> AssignDriver(
        Guid rideId,
        [FromBody] AssignRideDriverRequest request,
        CancellationToken cancellationToken) =>
        Send(new AssignRideDriverCommand(rideId, request.DriverId), cancellationToken, "تم تعيين الكابتن");

    [HttpDelete("{rideId:guid}/driver")]
    public Task<ActionResult<ApiResponse<RideDto>>> UnassignDriver(
        Guid rideId,
        CancellationToken cancellationToken) =>
        Send(new UnassignRideDriverCommand(rideId), cancellationToken, "تم إلغاء تعيين الكابتن");
}
