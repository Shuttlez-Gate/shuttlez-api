using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Rides;
using Shuttlez.Application.Rides.DTOs;
using Shuttlez.Application.RouteMatching.Models;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Rides.Handlers;

public record GetRideQuoteQuery(
    string? FromZoneKey,
    string? ToZoneKey,
    double? PickupLatitude = null,
    double? PickupLongitude = null,
    double? DestinationLatitude = null,
    double? DestinationLongitude = null) : IRequest<RideQuoteDto>;

public record GetRideFareOptionsQuery : IRequest<IReadOnlyList<RideFareOptionDto>>;

public record CreateRideCommand(CreateRideRequest Request) : IRequest<RideDto>;

public record GetMyRidesQuery : IRequest<IReadOnlyList<RideDto>>;

public record GetRideByIdQuery(Guid RideId) : IRequest<RideDto>;

public record CancelRideCommand(Guid RideId) : IRequest<RideDto>;

public class RideHandlers :
    IRequestHandler<GetRideQuoteQuery, RideQuoteDto>,
    IRequestHandler<GetRideFareOptionsQuery, IReadOnlyList<RideFareOptionDto>>,
    IRequestHandler<CreateRideCommand, RideDto>,
    IRequestHandler<GetMyRidesQuery, IReadOnlyList<RideDto>>,
    IRequestHandler<GetRideByIdQuery, RideDto>,
    IRequestHandler<CancelRideCommand, RideDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IRideFareResolver _fareResolver;
    private readonly IShuttleCommissionResolver _commissionResolver;
    private readonly ITripDistanceService _tripDistance;

    public RideHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IRideFareResolver fareResolver,
        IShuttleCommissionResolver commissionResolver,
        ITripDistanceService tripDistance)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _fareResolver = fareResolver;
        _commissionResolver = commissionResolver;
        _tripDistance = tripDistance;
    }

    public async Task<RideQuoteDto> Handle(
        GetRideQuoteQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = _clock.UtcNow;
        var rule = await _fareResolver.ResolveAsync(
            request.FromZoneKey, request.ToZoneKey, asOf, cancellationToken);
        var platformPercent = await _commissionResolver.GetPlatformCommissionPercentAsync(
            null, null, asOf, cancellationToken);

        var hasCoords = HasCoordinates(
            request.PickupLatitude,
            request.PickupLongitude,
            request.DestinationLatitude,
            request.DestinationLongitude);

        if (rule.PricePerKm > 0 && !hasCoords)
        {
            throw new AppException(
                "إحداثيات الانطلاق والوجهة مطلوبة لتسعير المشوار حسب المسافة.",
                400,
                ErrorCodes.TripCoordinatesRequired);
        }

        decimal? distanceKm = null;
        if (hasCoords)
        {
            distanceKm = await _tripDistance.GetDistanceKmAsync(
                new GeoCoordinate(request.PickupLatitude!.Value, request.PickupLongitude!.Value),
                new GeoCoordinate(request.DestinationLatitude!.Value, request.DestinationLongitude!.Value),
                cancellationToken);
        }

        var fare = rule.PricePerKm > 0
            ? DistanceBasedFareCalculator.CalculateRideFare(rule, distanceKm!.Value)
            : ShuttleFinancialCalculator.NormalizeMoney(rule.FlatFare);

        var (_, commission, captain) =
            ShuttleFinancialCalculator.SplitEarnings(fare, platformPercent);

        return new RideQuoteDto(
            rule.Id,
            rule.Name,
            rule.FromZoneKey,
            rule.ToZoneKey,
            fare,
            platformPercent,
            commission,
            captain,
            fare,
            CashPaymentPolicy.Cash,
            distanceKm,
            rule.PricePerKm > 0 ? rule.BaseFare : null,
            rule.PricePerKm > 0 ? rule.PricePerKm : null,
            rule.PricePerKm > 0 ? rule.MinimumFare : null);
    }

    public async Task<IReadOnlyList<RideFareOptionDto>> Handle(
        GetRideFareOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = _clock.UtcNow;
        return await _db.RideFareRules
            .AsNoTracking()
            .Where(r =>
                !r.IsDeleted &&
                r.IsActive &&
                (r.FlatFare > 0 || r.PricePerKm > 0) &&
                (r.EffectiveFrom == null || r.EffectiveFrom <= asOf) &&
                (r.EffectiveTo == null || r.EffectiveTo >= asOf))
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Select(r => new RideFareOptionDto(
                r.Id,
                r.Name,
                r.FromZoneKey,
                r.ToZoneKey,
                r.FlatFare,
                r.BaseFare,
                r.PricePerKm,
                r.MinimumFare))
            .ToListAsync(cancellationToken);
    }

    public async Task<RideDto> Handle(
        CreateRideCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var body = request.Request;
        var paymentMethod = RequireCashOnly(body.PaymentMethod);
        var asOf = _clock.UtcNow;

        var rule = await _fareResolver.ResolveAsync(
            body.FromZoneKey, body.ToZoneKey, asOf, cancellationToken);
        var platformPercent = await _commissionResolver.GetPlatformCommissionPercentAsync(
            null, null, asOf, cancellationToken);

        var distanceKm = await _tripDistance.GetDistanceKmAsync(
            new GeoCoordinate(body.PickupLatitude, body.PickupLongitude),
            new GeoCoordinate(body.DestinationLatitude, body.DestinationLongitude),
            cancellationToken);

        var fare = rule.PricePerKm > 0
            ? DistanceBasedFareCalculator.CalculateRideFare(rule, distanceKm)
            : ShuttleFinancialCalculator.NormalizeMoney(rule.FlatFare);

        var (rate, commission, captain) =
            ShuttleFinancialCalculator.SplitEarnings(fare, platformPercent);

        var ride = new RideRequest
        {
            RiderUserId = userId,
            PickupLatitude = body.PickupLatitude,
            PickupLongitude = body.PickupLongitude,
            PickupAddress = TrimOrNull(body.PickupAddress),
            DestinationLatitude = body.DestinationLatitude,
            DestinationLongitude = body.DestinationLongitude,
            DestinationAddress = TrimOrNull(body.DestinationAddress),
            FromZoneKey = TrimOrNull(body.FromZoneKey),
            ToZoneKey = TrimOrNull(body.ToZoneKey),
            RideFareRuleId = rule.Id,
            Status = RideRequestStatus.Requested,
            DistanceKm = distanceKm,
            BaseFareApplied = rule.PricePerKm > 0 ? rule.BaseFare : null,
            PricePerKmApplied = rule.PricePerKm > 0 ? rule.PricePerKm : null,
            MinimumFareApplied = rule.PricePerKm > 0 ? rule.MinimumFare : null,
            FareAmount = fare,
            CommissionRate = rate,
            CommissionAmount = commission,
            CaptainEarnings = captain,
            TotalAmount = fare,
            PaymentMethod = paymentMethod,
            IsCashConfirmed = true,
            ReferenceCode = $"RD-{asOf:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}"
        };

        _db.Add(ride);
        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectAsync(ride.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<RideDto>> Handle(
        GetMyRidesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var ids = await _db.RideRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.RiderUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var list = new List<RideDto>(ids.Count);
        foreach (var id in ids)
            list.Add(await ProjectAsync(id, cancellationToken));
        return list;
    }

    public async Task<RideDto> Handle(
        GetRideByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var ride = await _db.RideRequests
            .AsNoTracking()
            .Where(r => r.Id == request.RideId && !r.IsDeleted)
            .Select(r => new { r.Id, r.RiderUserId, r.DriverId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("المشوار غير موجود", ErrorCodes.RideNotFound);

        if (!_currentUser.IsAdmin && ride.RiderUserId != userId)
        {
            var isAssignedDriver = ride.DriverId is not null && await _db.Drivers
                .AsNoTracking()
                .AnyAsync(
                    d => d.Id == ride.DriverId && d.UserId == userId && !d.IsDeleted,
                    cancellationToken);

            if (!isAssignedDriver)
                throw new ForbiddenAppException("غير مصرح بعرض هذا المشوار");
        }

        return await ProjectAsync(ride.Id, cancellationToken);
    }

    public async Task<RideDto> Handle(
        CancelRideCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var now = _clock.UtcNow;

        var ride = await _db.RideRequests
            .FirstOrDefaultAsync(r => r.Id == request.RideId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("المشوار غير موجود", ErrorCodes.RideNotFound);

        if (!_currentUser.IsAdmin && ride.RiderUserId != userId)
            throw new ForbiddenAppException("غير مصرح بإلغاء هذا المشوار");

        if (ride.Status == RideRequestStatus.Cancelled)
            return await ProjectAsync(ride.Id, cancellationToken);

        if (ride.Status is not (RideRequestStatus.Requested or RideRequestStatus.Assigned))
        {
            throw new AppException(
                "لا يمكن إلغاء المشوار في حالته الحالية",
                400,
                ErrorCodes.RideNotCancellable);
        }

        ride.Status = RideRequestStatus.Cancelled;
        ride.CancelledAt ??= now;
        ride.UpdatedAt = now;
        _db.Update(ride);
        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectAsync(ride.Id, cancellationToken);
    }

    private Task<RideDto> ProjectAsync(Guid rideId, CancellationToken ct) =>
        RideDtoProjector.ProjectAsync(_db, rideId, ct);

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAppException("غير مصرح");

    private static string RequireCashOnly(string? raw)
    {
        var method = CashPaymentPolicy.NormalizeOrThrow(raw);
        if (!CashPaymentPolicy.IsCash(method))
        {
            throw new AppException(
                "المشوار يقبل الدفع نقدًا للكابتن فقط.",
                400,
                ErrorCodes.RideCashRequired);
        }

        return method;
    }

    private static bool HasCoordinates(
        double? pickupLatitude,
        double? pickupLongitude,
        double? destinationLatitude,
        double? destinationLongitude) =>
        pickupLatitude.HasValue &&
        pickupLongitude.HasValue &&
        destinationLatitude.HasValue &&
        destinationLongitude.HasValue;

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
