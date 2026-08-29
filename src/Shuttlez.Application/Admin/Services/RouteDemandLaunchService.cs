using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Pricing;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Services;

public sealed class RouteDemandLaunchService : IRouteDemandLaunchService
{
    private readonly IAppDbContext _db;
    private readonly IRouteDemandAnalysisService _analysis;
    private readonly IPricingRuleResolver _pricing;
    private readonly IShuttleCommissionResolver _commission;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<RouteDemandLaunchService> _logger;

    public RouteDemandLaunchService(
        IAppDbContext db,
        IRouteDemandAnalysisService analysis,
        IPricingRuleResolver pricing,
        IShuttleCommissionResolver commission,
        IDateTimeProvider clock,
        ILogger<RouteDemandLaunchService> logger)
    {
        _db = db;
        _analysis = analysis;
        _pricing = pricing;
        _commission = commission;
        _clock = clock;
        _logger = logger;
    }

    public async Task<RouteDemandLaunchResultDto> LaunchAsync(
        string routeKey,
        LaunchRouteDemandRequest request,
        Guid? adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(routeKey))
            throw new AppException("مفتاح مجموعة الطلب مطلوب.", code: ErrorCodes.RouteDemandNotFound);

        var scheduledAt = ToUtc(request.ScheduledAt);
        RouteDemandLaunchEligibility.EnsureScheduledAtValid(scheduledAt, _clock.UtcNow);

        // Re-evaluate readiness at launch time — never trust Angular.
        var details = await _analysis.GetDetailsAsync(routeKey, cancellationToken);
        RouteDemandLaunchEligibility.EnsureReadyToLaunch(details);

        var readiness = details!.Readiness!;
        var routeId = readiness.RouteId!.Value;
        var routeExists = await _db.Routes.AnyAsync(
            r => r.Id == routeId && !r.IsDeleted && r.IsActive,
            cancellationToken);
        if (!routeExists)
            throw new AppException("المسار الرسمي غير موجود أو غير نشط.", code: ErrorCodes.RouteNotFound);

        var (vehicleType, capacity, vehicleId) = await ResolveVehicleAsync(
            readiness.VehicleType,
            readiness.Capacity,
            request.VehicleId,
            request.DriverId,
            cancellationToken);

        if (capacity <= 0)
            throw new AppException("سعة المركبة غير متاحة.", code: ErrorCodes.NoVehicleConfig);

        var rule = await _pricing.FindActiveRuleAsync(routeId, vehicleType, _clock.UtcNow, cancellationToken);
        if (rule is null || rule.OneWayPrice <= 0)
            throw new AppException("لا يوجد تسعير فعّال لهذا الخط ونوع المركبة.", code: ErrorCodes.PricingNotConfigured);

        var pricePerSeat = ShuttlePricingCalculator.NormalizeMoney(rule.OneWayPrice);
        var commissionPercent = await _commission.GetPlatformCommissionPercentAsync(
            routeId,
            vehicleType,
            _clock.UtcNow,
            cancellationToken);

        Guid? driverId = null;
        if (request.DriverId is Guid did && did != Guid.Empty)
        {
            var driverOk = await _db.Drivers.AnyAsync(
                d => d.Id == did && !d.IsDeleted && d.IsActive,
                cancellationToken);
            if (!driverOk)
                throw new AppException("الكابتن غير موجود أو غير نشط.", code: ErrorCodes.DriverNotFound);
            driverId = did;
        }

        var lockKey = BuildLockKey(routeId, scheduledAt);

        try
        {
            return await _db.ExecuteInSerializableTransactionAsync(async ct =>
            {
                await _db.AcquireTransactionAdvisoryLockAsync(lockKey, ct);

                var duplicate = await _db.Trips.AnyAsync(
                    t => !t.IsDeleted &&
                         t.RouteId == routeId &&
                         t.ScheduledAt == scheduledAt &&
                         t.Status != TripStatus.Cancelled,
                    ct);
                if (duplicate)
                {
                    throw new AppException(
                        "توجد رحلة تشغيلية لنفس الخط والموعد بالفعل.",
                        code: ErrorCodes.DuplicateOperationalTrip);
                }

                var trip = new Trip
                {
                    RouteId = routeId,
                    DriverId = driverId,
                    ScheduledAt = scheduledAt,
                    PricePerSeat = pricePerSeat,
                    AvailableSeats = capacity,
                    Status = driverId is null ? TripStatus.Scheduled : TripStatus.DriverAssigned,
                    ReferenceCode = BuildReferenceCode(),
                };
                _db.Add(trip);
                await _db.SaveChangesAsync(ct);

                // Demand is NOT converted to bookings. Historical bookings untouched.
                _logger.LogInformation(
                    "Route demand launched. DemandKey={RouteKey} RouteId={RouteId} TripId={TripId} AdminUserId={AdminUserId}",
                    routeKey,
                    routeId,
                    trip.Id,
                    adminUserId);

                return new RouteDemandLaunchResultDto(
                    trip.Id,
                    trip.RouteId,
                    details.RouteLabel,
                    trip.DriverId,
                    vehicleId,
                    vehicleType.ToString(),
                    trip.Status.ToString(),
                    trip.ScheduledAt,
                    trip.PricePerSeat,
                    trip.AvailableSeats,
                    commissionPercent,
                    trip.ReferenceCode,
                    trip.CreatedAt,
                    "تم إنشاء الرحلة التشغيلية. لن يتم تحويل طلبات الطلب إلى حجوزات تلقائياً.");
            }, cancellationToken);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(
                "Route demand launch failed. DemandKey={RouteKey} RouteId={RouteId} AdminUserId={AdminUserId} Code={Code}",
                routeKey,
                routeId,
                adminUserId,
                ex.Code);
            throw;
        }
    }

    private async Task<(VehicleType Type, int Capacity, Guid? VehicleId)> ResolveVehicleAsync(
        string? readinessVehicleType,
        int? readinessCapacity,
        Guid? requestVehicleId,
        Guid? requestDriverId,
        CancellationToken cancellationToken)
    {
        if (requestVehicleId is Guid vid && vid != Guid.Empty)
        {
            var vehicle = await _db.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vid && !v.IsDeleted && v.IsActive, cancellationToken);
            if (vehicle is null)
                throw new AppException("المركبة غير موجودة أو غير نشطة.", code: ErrorCodes.NoVehicleAvailable);
            if (vehicle.Capacity <= 0)
                throw new AppException("سعة المركبة غير صالحة.", code: ErrorCodes.NoVehicleConfig);
            return (vehicle.Type, vehicle.Capacity, vehicle.Id);
        }

        if (requestDriverId is Guid did && did != Guid.Empty)
        {
            var driverVehicleId = await _db.Drivers
                .AsNoTracking()
                .Where(d => d.Id == did && !d.IsDeleted)
                .Select(d => d.VehicleId)
                .FirstOrDefaultAsync(cancellationToken);
            if (driverVehicleId is Guid linkedVehicleId)
            {
                var linked = await _db.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        v => v.Id == linkedVehicleId && !v.IsDeleted && v.IsActive,
                        cancellationToken);
                if (linked is not null && linked.Capacity > 0)
                    return (linked.Type, linked.Capacity, linked.Id);
            }
        }

        if (!Enum.TryParse<VehicleType>(readinessVehicleType, ignoreCase: true, out var vt))
            throw new AppException("نوع المركبة غير متاح بشكل موثوق.", code: ErrorCodes.NoVehicleConfig);

        if (readinessCapacity is int cap && cap > 0)
            return (vt, cap, null);

        var caps = await _analysis.GetVehicleCapacitiesAsync(cancellationToken);
        var match = caps.FirstOrDefault(c =>
            string.Equals(c.VehicleType, vt.ToString(), StringComparison.OrdinalIgnoreCase));
        if (match is null || match.Capacity <= 0)
            throw new AppException("لا توجد سعة مركبة معتمدة.", code: ErrorCodes.NoVehicleConfig);

        return (vt, match.Capacity, null);
    }

    private static long BuildLockKey(Guid routeId, DateTime scheduledAtUtc)
    {
        var payload = routeId.ToString("N") + "|" + scheduledAtUtc.Ticks.ToString(CultureInfo.InvariantCulture);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToInt64(hash, 0);
    }

    private static string BuildReferenceCode() =>
        "TRP-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
