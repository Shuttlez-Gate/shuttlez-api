using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers.Admin;

[Route("api/v1/admin/trips")]
public class AdminTripsController : AdminControllerBase
{
    public AdminTripsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminTripDto>>>> List(
        [FromQuery] Guid? routeId,
        [FromQuery] Guid? driverId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminTripsQuery(routeId, driverId, status, from, to, page, pageSize), cancellationToken);

    [HttpPost]
    public Task<ActionResult<ApiResponse<AdminTripDto>>> Create(
        [FromBody] SaveTripRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveTripCommand(null, request), cancellationToken, "تم إنشاء الرحلة");

    [HttpPut("{id:guid}")]
    public Task<ActionResult<ApiResponse<AdminTripDto>>> Update(
        Guid id,
        [FromBody] SaveTripRequest request,
        CancellationToken cancellationToken) =>
        Send(new SaveTripCommand(id, request), cancellationToken, "تم تحديث الرحلة");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteTripCommand(id), cancellationToken, "تم إلغاء الرحلة");

    /// <summary>جدولة دفعة رحلات متكرّرة على خط واحد.</summary>
    [HttpPost("generate")]
    public Task<ActionResult<ApiResponse<int>>> Generate(
        [FromBody] GenerateTripsRequest request,
        CancellationToken cancellationToken) =>
        Send(new GenerateTripsCommand(request), cancellationToken, "تم جدولة الرحلات");
}

[Route("api/v1/admin/bookings")]
public class AdminBookingsController : AdminControllerBase
{
    public AdminBookingsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminBookingDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] Guid? tripId,
        [FromQuery] Guid? userId,
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminBookingsQuery(search, tripId, userId, status, page, pageSize), cancellationToken);

    [HttpPatch("{id:guid}/status")]
    public Task<ActionResult<ApiResponse<AdminBookingDto>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateBookingStatusRequest request,
        CancellationToken cancellationToken) =>
        Send(new UpdateBookingStatusCommand(id, request), cancellationToken, "تم تحديث الحجز");
}

[Route("api/v1/admin/reviews")]
public class AdminReviewsController : AdminControllerBase
{
    public AdminReviewsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminReviewDto>>>> List(
        [FromQuery] Guid? driverId,
        [FromQuery] int? minStars,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminReviewsQuery(driverId, minStars, page, pageSize), cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteReviewCommand(id), cancellationToken, "تم حذف التقييم");
}

[Route("api/v1/admin/route-requests")]
public class AdminRouteRequestsController : AdminControllerBase
{
    public AdminRouteRequestsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminRouteRequestDto>>>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminRouteRequestsQuery(search, status, page, pageSize), cancellationToken);

    [HttpPatch("{id:guid}/status")]
    public Task<ActionResult<ApiResponse<AdminRouteRequestDto>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateRouteRequestStatusRequest request,
        CancellationToken cancellationToken) =>
        Send(new UpdateRouteRequestStatusCommand(id, request), cancellationToken, "تم تحديث الطلب");

    [HttpDelete("{id:guid}")]
    public Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        Send(new DeleteRouteRequestCommand(id), cancellationToken, "تم حذف الطلب");
}

[Route("api/v1/admin/leads")]
public class AdminLeadsController : AdminControllerBase
{
    public AdminLeadsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public Task<ActionResult<ApiResponse<PagedResult<AdminLandingLeadDto>>>> List(
        [FromQuery] string? kind,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        Send(new AdminLandingLeadsQuery(kind, search, page, pageSize), cancellationToken);
}
