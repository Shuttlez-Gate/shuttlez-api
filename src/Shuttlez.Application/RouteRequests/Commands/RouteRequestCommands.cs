using System.Text.Json;
using MediatR;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Landing.Services;
using Shuttlez.Application.RouteRequests.DTOs;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.RouteRequests.Commands;

public record GetRouteRequestOptionsQuery(string? Language = null) : IRequest<RouteRequestOptionsDto>;

public record CreateRouteRequestCommand(CreateRouteRequestDto Request)
    : IRequest<CreateRouteRequestResponse>;

public class RouteRequestHandlers :
    IRequestHandler<GetRouteRequestOptionsQuery, RouteRequestOptionsDto>,
    IRequestHandler<CreateRouteRequestCommand, CreateRouteRequestResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RouteRequestHandlers(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<RouteRequestOptionsDto> Handle(
        GetRouteRequestOptionsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(LandingRouteOptionsProvider.Build(request.Language));

    public async Task<CreateRouteRequestResponse> Handle(
        CreateRouteRequestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var form = request.Request;
        if (string.IsNullOrWhiteSpace(form.FromCity) ||
            string.IsNullOrWhiteSpace(form.FromRegion) ||
            string.IsNullOrWhiteSpace(form.ToCity) ||
            string.IsNullOrWhiteSpace(form.ToRegion))
        {
            throw new AppException("يرجى إكمال بيانات المسار");
        }

        var vehicleType = NormalizeVehicleType(form.PreferredVehicleType);

        if (!EgyptAreaCoordinates.HasValidCoordinates(
                form.FromLatitude ?? 0, form.FromLongitude ?? 0) ||
            !EgyptAreaCoordinates.HasValidCoordinates(
                form.ToLatitude ?? 0, form.ToLongitude ?? 0))
        {
            throw new AppException(
                "حدّد نقطة الانطلاق والوصول بدقة على الخريطة");
        }

        var notes = JsonSerializer.Serialize(new
        {
            form.FromTime,
            form.ToTime,
            form.WeeklyCount,
            form.UsageDays,
            form.UsageReason,
            PreferredVehicleType = vehicleType,
        });

        var entity = new RouteRequest
        {
            UserId = userId,
            FromAddress = $"{form.FromRegion}, {form.FromCity}",
            ToAddress = $"{form.ToRegion}, {form.ToCity}",
            FromLatitude = form.FromLatitude!.Value,
            FromLongitude = form.FromLongitude!.Value,
            ToLatitude = form.ToLatitude!.Value,
            ToLongitude = form.ToLongitude!.Value,
            PreferredVehicleType = vehicleType,
            Status = "pending",
            Notes = notes,
        };

        _db.Add(entity);
        _db.Add(new Notification
        {
            UserId = userId,
            Title = "تم استلام طلب مسار جديد",
            Body = $"سنراجع طلبك من {entity.FromAddress} إلى {entity.ToAddress}.",
            Type = "route_request",
            IsRead = false,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateRouteRequestResponse(
            entity.Id,
            "تم إرسال طلب المسار بنجاح");
    }

    private static string NormalizeVehicleType(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "car" or "carshuttle" or "car_shuttle" => "carshuttle",
            "bus" => "bus",
            _ => "minibus",
        };
}
