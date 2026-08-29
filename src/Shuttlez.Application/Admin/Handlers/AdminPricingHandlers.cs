using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Bookings;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Pricing;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminPricingRuleDto(
    Guid Id,
    string Name,
    Guid? RouteId,
    string? RouteName,
    string VehicleType,
    decimal OneWayPrice,
    decimal RoundTripPrice,
    decimal WeeklyPrice,
    decimal MonthlyPrice,
    decimal LaunchCommissionPercent,
    decimal PermanentCommissionPercent,
    int LaunchPeriodDays,
    DateTime? LaunchStartAt,
    int MinimumLaunchRiders,
    int TargetOccupancy,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive);

public record SavePricingRuleRequest(
    string Name,
    Guid? RouteId,
    string VehicleType,
    decimal OneWayPrice,
    decimal RoundTripPrice,
    decimal WeeklyPrice,
    decimal MonthlyPrice,
    decimal LaunchCommissionPercent,
    decimal PermanentCommissionPercent,
    int LaunchPeriodDays,
    DateTime? LaunchStartAt,
    int MinimumLaunchRiders,
    int TargetOccupancy,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive = true);

public record AdminPricingListQuery(
    Guid? RouteId = null,
    string? VehicleType = null,
    bool? ActiveOnly = null) : IRequest<IReadOnlyList<AdminPricingRuleDto>>;

public record SavePricingRuleCommand(Guid? Id, SavePricingRuleRequest Request)
    : IRequest<AdminPricingRuleDto>;

public record DeletePricingRuleCommand(Guid Id) : IRequest<bool>;

public record PricingPreviewRequest(
    Guid? RouteId,
    string VehicleType,
    int PassengerCount,
    string TripType,
    int? Capacity = null);

public record PricingPreviewDto(
    Guid? PricingRuleId,
    string VehicleType,
    string TripType,
    int PassengerCount,
    decimal UnitPrice,
    decimal GrossRevenue,
    decimal CommissionPercent,
    decimal CommissionAmount,
    decimal CaptainEarnings,
    decimal OccupancyPercent,
    int? MinimumLaunchRiders,
    int? TargetOccupancy,
    string? LaunchStatus,
    bool IsLaunchPeriod);

public record PricingPreviewQuery(PricingPreviewRequest Request) : IRequest<PricingPreviewDto>;

public record CaptainEarningsRowDto(
    Guid BookingId,
    Guid TripId,
    Guid? DriverId,
    string? CaptainName,
    string RouteName,
    string? VehicleType,
    int SeatCount,
    decimal PricePerSeat,
    decimal GrossRevenue,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal CaptainEarnings,
    string BookingStatus,
    DateTime ScheduledAt,
    DateTime CreatedAt);

public record CaptainEarningsReportDto(
    IReadOnlyList<CaptainEarningsRowDto> Items,
    decimal TotalGross,
    decimal TotalCommission,
    decimal TotalCaptainEarnings,
    int TotalSeats);

public record CaptainEarningsReportQuery(
    DateTime? From = null,
    DateTime? To = null,
    Guid? DriverId = null,
    Guid? RouteId = null) : IRequest<CaptainEarningsReportDto>;

public class AdminPricingHandlers :
    IRequestHandler<AdminPricingListQuery, IReadOnlyList<AdminPricingRuleDto>>,
    IRequestHandler<SavePricingRuleCommand, AdminPricingRuleDto>,
    IRequestHandler<DeletePricingRuleCommand, bool>,
    IRequestHandler<PricingPreviewQuery, PricingPreviewDto>,
    IRequestHandler<CaptainEarningsReportQuery, CaptainEarningsReportDto>
{
    private readonly IAppDbContext _db;
    private readonly IPricingRuleResolver _pricing;
    private readonly IDateTimeProvider _clock;

    private static readonly Dictionary<VehicleType, int> DefaultCapacityHints = new()
    {
        [VehicleType.CarShuttle] = 4,
        [VehicleType.MiniBus] = 13,
        [VehicleType.Bus] = 24
    };

    public AdminPricingHandlers(
        IAppDbContext db,
        IPricingRuleResolver pricing,
        IDateTimeProvider clock)
    {
        _db = db;
        _pricing = pricing;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AdminPricingRuleDto>> Handle(
        AdminPricingListQuery request,
        CancellationToken cancellationToken)
    {
        var q = _db.PricingRules
            .AsNoTracking()
            .Include(r => r.Route)
            .Where(r => !r.IsDeleted);

        if (request.RouteId is Guid rid)
            q = q.Where(r => r.RouteId == rid);
        if (!string.IsNullOrWhiteSpace(request.VehicleType) &&
            TryParseVehicle(request.VehicleType, out var vt))
            q = q.Where(r => r.VehicleType == vt);
        if (request.ActiveOnly == true)
            q = q.Where(r => r.IsActive);

        var list = await q
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.VehicleType)
            .ThenBy(r => r.Route != null ? r.Route.Name : "")
            .ToListAsync(cancellationToken);

        return list.Select(Map).ToList();
    }

    public async Task<AdminPricingRuleDto> Handle(
        SavePricingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        if (string.IsNullOrWhiteSpace(body.Name))
            throw new AppException("اسم قاعدة التسعير مطلوب");
        if (!TryParseVehicle(body.VehicleType, out var vehicleType))
            throw new AppException("نوع المركبة غير صالح");

        ValidateMoney(body.OneWayPrice, nameof(body.OneWayPrice));
        ValidateMoney(body.RoundTripPrice, nameof(body.RoundTripPrice));
        ValidateMoney(body.WeeklyPrice, nameof(body.WeeklyPrice));
        ValidateMoney(body.MonthlyPrice, nameof(body.MonthlyPrice));
        ValidatePercent(body.LaunchCommissionPercent);
        ValidatePercent(body.PermanentCommissionPercent);

        if (body.LaunchPeriodDays < 0)
            throw new AppException("مدة الإطلاق يجب أن تكون ≥ 0");
        if (body.MinimumLaunchRiders < 1)
            throw new AppException("الحد الأدنى للإطلاق يجب أن يكون ≥ 1");
        if (body.TargetOccupancy < body.MinimumLaunchRiders)
            throw new AppException("الهدف يجب أن يكون ≥ الحد الأدنى للإطلاق");

        var capacityHint = await ResolveCapacityHintAsync(
            body.RouteId,
            vehicleType,
            cancellationToken);
        if (body.TargetOccupancy > capacityHint)
            throw new AppException(
                $"الهدف لا يمكن أن يتجاوز سعة المركبة ({capacityHint})");

        if (body.RouteId is Guid routeId)
        {
            var routeExists = await _db.Routes.AnyAsync(
                r => r.Id == routeId && !r.IsDeleted,
                cancellationToken);
            if (!routeExists)
                throw new NotFoundException("الخط غير موجود");
        }

        PricingRule rule;
        if (request.Id is null)
        {
            rule = new PricingRule();
            _db.Add(rule);
        }
        else
        {
            rule = await _db.PricingRules
                .FirstOrDefaultAsync(
                    r => r.Id == request.Id && !r.IsDeleted,
                    cancellationToken)
                ?? throw new NotFoundException("قاعدة التسعير غير موجودة");
        }

        rule.Name = body.Name.Trim();
        rule.RouteId = body.RouteId;
        rule.VehicleType = vehicleType;
        rule.OneWayPrice = ShuttleFinancialCalculator.NormalizeMoney(body.OneWayPrice);
        rule.RoundTripPrice = ShuttleFinancialCalculator.NormalizeMoney(body.RoundTripPrice);
        rule.WeeklyPrice = ShuttleFinancialCalculator.NormalizeMoney(body.WeeklyPrice);
        rule.MonthlyPrice = ShuttleFinancialCalculator.NormalizeMoney(body.MonthlyPrice);
        rule.LaunchCommissionPercent = body.LaunchCommissionPercent;
        rule.PermanentCommissionPercent = body.PermanentCommissionPercent;
        rule.LaunchPeriodDays = body.LaunchPeriodDays;
        rule.LaunchStartAt = body.LaunchStartAt;
        rule.MinimumLaunchRiders = body.MinimumLaunchRiders;
        rule.TargetOccupancy = body.TargetOccupancy;
        rule.EffectiveFrom = body.EffectiveFrom;
        rule.EffectiveTo = body.EffectiveTo;
        rule.IsActive = body.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        string? routeName = null;
        if (rule.RouteId is Guid rid)
        {
            routeName = await _db.Routes
                .AsNoTracking()
                .Where(r => r.Id == rid)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new AdminPricingRuleDto(
            rule.Id,
            rule.Name,
            rule.RouteId,
            routeName,
            rule.VehicleType.ToString(),
            rule.OneWayPrice,
            rule.RoundTripPrice,
            rule.WeeklyPrice,
            rule.MonthlyPrice,
            rule.LaunchCommissionPercent,
            rule.PermanentCommissionPercent,
            rule.LaunchPeriodDays,
            rule.LaunchStartAt,
            rule.MinimumLaunchRiders,
            rule.TargetOccupancy,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.IsActive);
    }

    public async Task<bool> Handle(
        DeletePricingRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await _db.PricingRules
            .FirstOrDefaultAsync(
                r => r.Id == request.Id && !r.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("قاعدة التسعير غير موجودة");

        rule.IsDeleted = true;
        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PricingPreviewDto> Handle(
        PricingPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        if (body.PassengerCount <= 0)
            throw new AppException("عدد الركاب غير صالح", 400, ErrorCodes.InvalidSeatCount);
        if (!TryParseVehicle(body.VehicleType, out var vehicleType))
            throw new AppException("نوع المركبة غير صالح");

        var tripType = ParseTripType(body.TripType);
        var asOf = _clock.UtcNow;
        var rule = await _pricing.FindActiveRuleAsync(
            body.RouteId,
            vehicleType,
            asOf,
            cancellationToken)
            ?? throw new AppException(
                "لا توجد قاعدة تسعير نشطة لهذا الخط/المركبة",
                400,
                ErrorCodes.PricingNotAvailable);

        var unit = ShuttlePricingCalculator.ResolveUnitPrice(
            rule.OneWayPrice,
            rule.RoundTripPrice,
            tripType);
        var gross = ShuttlePricingCalculator.CalculateGross(unit, body.PassengerCount);

        var launchStart = ShuttlePricingCalculator.ResolveLaunchStart(
            rule.LaunchStartAt,
            rule.EffectiveFrom,
            rule.CreatedAt);
        var commissionPct = ShuttlePricingCalculator.ResolveCommissionPercent(
            rule.LaunchCommissionPercent,
            rule.PermanentCommissionPercent,
            rule.LaunchPeriodDays,
            launchStart,
            asOf);
        var isLaunch = commissionPct == Math.Clamp(rule.LaunchCommissionPercent, 0m, 100m)
                       && rule.LaunchPeriodDays > 0
                       && asOf < launchStart.AddDays(rule.LaunchPeriodDays);

        var (rate, commission, captain) =
            ShuttleFinancialCalculator.SplitEarnings(gross, commissionPct);

        var capacity = body.Capacity
            ?? await ResolveCapacityHintAsync(body.RouteId, vehicleType, cancellationToken);
        var occupancy = ShuttlePricingCalculator.OccupancyPercent(body.PassengerCount, capacity);
        var status = ShuttlePricingCalculator.ResolveLaunchStatus(
            body.PassengerCount,
            rule.MinimumLaunchRiders,
            rule.TargetOccupancy,
            capacity);

        return new PricingPreviewDto(
            rule.Id,
            vehicleType.ToString(),
            tripType.ToString(),
            body.PassengerCount,
            unit,
            gross,
            commissionPct,
            commission,
            captain,
            occupancy,
            rule.MinimumLaunchRiders,
            rule.TargetOccupancy,
            status.ToString(),
            isLaunch);
    }

    public async Task<CaptainEarningsReportDto> Handle(
        CaptainEarningsReportQuery request,
        CancellationToken cancellationToken)
    {
        var q = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Trip)
                .ThenInclude(t => t.Route)
            .Include(b => b.Trip)
                .ThenInclude(t => t.Driver!)
                    .ThenInclude(d => d.User)
            .Include(b => b.Trip)
                .ThenInclude(t => t.Driver!)
                    .ThenInclude(d => d.Vehicle)
            .Where(b =>
                !b.IsDeleted &&
                b.Status == BookingStatus.Confirmed);

        if (request.From is DateTime from)
            q = q.Where(b => b.Trip.ScheduledAt >= from);
        if (request.To is DateTime to)
            q = q.Where(b => b.Trip.ScheduledAt < to);
        if (request.DriverId is Guid driverId)
            q = q.Where(b => b.Trip.DriverId == driverId);
        if (request.RouteId is Guid routeId)
            q = q.Where(b => b.Trip.RouteId == routeId);

        var rows = await q
            .OrderByDescending(b => b.Trip.ScheduledAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        var items = rows.Select(b =>
        {
            var gross = b.TotalAmount;
            var commission = b.PricePerSeat > 0 ? b.CommissionAmount : 0m;
            var captain = b.PricePerSeat > 0 ? b.CaptainEarnings : b.TotalAmount;
            return new CaptainEarningsRowDto(
                b.Id,
                b.TripId,
                b.Trip.DriverId,
                b.Trip.Driver?.User?.FullName,
                b.Trip.Route.Name,
                b.Trip.Driver?.Vehicle?.Type.ToString(),
                b.SeatCount,
                b.PricePerSeat > 0 ? b.PricePerSeat : (b.SeatCount > 0 ? b.TotalAmount / b.SeatCount : 0),
                gross,
                b.CommissionRate,
                commission,
                captain,
                b.Status.ToString(),
                b.Trip.ScheduledAt,
                b.CreatedAt);
        }).ToList();

        return new CaptainEarningsReportDto(
            items,
            items.Sum(i => i.GrossRevenue),
            items.Sum(i => i.CommissionAmount),
            items.Sum(i => i.CaptainEarnings),
            items.Sum(i => i.SeatCount));
    }

    private async Task<int> ResolveCapacityHintAsync(
        Guid? routeId,
        VehicleType vehicleType,
        CancellationToken cancellationToken)
    {
        var fromFleet = await _db.Vehicles
            .AsNoTracking()
            .Where(v =>
                !v.IsDeleted &&
                v.IsActive &&
                v.Type == vehicleType &&
                v.Capacity > 0)
            .OrderByDescending(v => v.Capacity)
            .Select(v => (int?)v.Capacity)
            .FirstOrDefaultAsync(cancellationToken);

        if (fromFleet is > 0)
            return fromFleet.Value;

        return DefaultCapacityHints.GetValueOrDefault(vehicleType, 4);
    }

    private static AdminPricingRuleDto Map(PricingRule r) => new(
        r.Id,
        r.Name,
        r.RouteId,
        r.Route?.Name,
        r.VehicleType.ToString(),
        r.OneWayPrice,
        r.RoundTripPrice,
        r.WeeklyPrice,
        r.MonthlyPrice,
        r.LaunchCommissionPercent,
        r.PermanentCommissionPercent,
        r.LaunchPeriodDays,
        r.LaunchStartAt,
        r.MinimumLaunchRiders,
        r.TargetOccupancy,
        r.EffectiveFrom,
        r.EffectiveTo,
        r.IsActive);

    private static void ValidateMoney(decimal value, string field)
    {
        if (value < 0)
            throw new AppException($"{field}: السعر يجب أن يكون ≥ 0");
    }

    private static void ValidatePercent(decimal value)
    {
        if (value < 0 || value > 100)
            throw new AppException("نسبة العمولة يجب أن تكون بين 0 و 100");
    }

    private static bool TryParseVehicle(string raw, out VehicleType type)
    {
        type = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var key = raw.Trim();
        if (Enum.TryParse(key, ignoreCase: true, out type) &&
            Enum.IsDefined(typeof(VehicleType), type))
            return true;

        type = key.ToLowerInvariant() switch
        {
            "car" or "shuttlecar" or "shuttlezcar" or "car_shuttle" or "carshuttle" =>
                VehicleType.CarShuttle,
            "microbus" or "minibus" or "mini_bus" => VehicleType.MiniBus,
            "bus" => VehicleType.Bus,
            _ => default
        };
        return type != default;
    }

    private static PricingTripType ParseTripType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return PricingTripType.OneWay;
        var key = raw.Trim().Replace("-", "").Replace("_", "").ToUpperInvariant();
        return key is "ROUNDTRIP" or "ROUND" or "RT"
            ? PricingTripType.RoundTrip
            : PricingTripType.OneWay;
    }
}
