using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Landing.DTOs;
using Shuttlez.Application.Landing.Services;
using Shuttlez.Application.RouteRequests.DTOs;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Landing.Handlers;

public record GetLandingRouteOptionsQuery(string? Language = null) : IRequest<RouteRequestOptionsDto>;

public record SubmitLandingRouteLeadCommand(LandingRouteLeadDto Request)
    : IRequest<LandingSubmitResponse>;

public record SubmitLandingWaitlistCommand(LandingWaitlistDto Request)
    : IRequest<LandingSubmitResponse>;

public record SubmitLandingCaptainLeadCommand(LandingCaptainLeadDto Request)
    : IRequest<LandingSubmitResponse>;

public class LandingCommandHandlers :
    IRequestHandler<GetLandingRouteOptionsQuery, RouteRequestOptionsDto>,
    IRequestHandler<SubmitLandingRouteLeadCommand, LandingSubmitResponse>,
    IRequestHandler<SubmitLandingWaitlistCommand, LandingSubmitResponse>,
    IRequestHandler<SubmitLandingCaptainLeadCommand, LandingSubmitResponse>
{
    private readonly IAppDbContext _db;

    public LandingCommandHandlers(IAppDbContext db) => _db = db;

    public Task<RouteRequestOptionsDto> Handle(
        GetLandingRouteOptionsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(LandingRouteOptionsProvider.Build(request.Language));

    public async Task<LandingSubmitResponse> Handle(
        SubmitLandingRouteLeadCommand request,
        CancellationToken cancellationToken)
    {
        var form = request.Request;
        ValidatePhone(form.Phone);

        if (string.IsNullOrWhiteSpace(form.FromCity) ||
            string.IsNullOrWhiteSpace(form.FromRegion) ||
            string.IsNullOrWhiteSpace(form.ToCity) ||
            string.IsNullOrWhiteSpace(form.ToRegion))
        {
            throw new AppException("يرجى إكمال بيانات المسار");
        }

        var entity = new LandingRouteLead
        {
            Phone = NormalizePhone(form.Phone),
            FromCity = form.FromCity.Trim(),
            FromRegion = form.FromRegion.Trim(),
            FromTime = form.FromTime,
            ToCity = form.ToCity.Trim(),
            ToRegion = form.ToRegion.Trim(),
            ToTime = form.ToTime,
            WeeklyCount = form.WeeklyCount <= 0 ? 5 : form.WeeklyCount,
            UsageDays = form.UsageDays,
            UsageReason = form.UsageReason,
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new LandingSubmitResponse(
            entity.Id,
            "تم تسجيل طلبك بنجاح. سنتواصل معك قريباً.");
    }

    public async Task<LandingSubmitResponse> Handle(
        SubmitLandingWaitlistCommand request,
        CancellationToken cancellationToken)
    {
        ValidatePhone(request.Request.Phone);
        var phone = NormalizePhone(request.Request.Phone);
        var form = request.Request;

        Route? route = null;
        if (form.RouteId is Guid routeId)
        {
            route = await _db.Routes
                .FirstOrDefaultAsync(
                    r => r.Id == routeId && r.IsActive && !r.IsDeleted,
                    cancellationToken)
                ?? throw new AppException("المسار غير موجود");

            var existsForRoute = await _db.LandingWaitlistEntries
                .AnyAsync(
                    x => x.Phone == phone && x.RouteId == routeId,
                    cancellationToken);
            if (existsForRoute)
                throw new AppException("رقمك مسجل بالفعل في قائمة انتظار هذا المسار");
        }
        else
        {
            var exists = await _db.LandingWaitlistEntries
                .AnyAsync(x => x.Phone == phone && x.RouteId == null, cancellationToken);
            if (exists)
                throw new AppException("رقمك مسجل بالفعل في قائمة الانتظار");
        }

        var (routeFrom, routeTo) = route is null
            ? (form.RouteFrom?.Trim(), form.RouteTo?.Trim())
            : LandingLabelLocalizer.LocalizeRouteName(route.Name, null);

        var entity = new LandingWaitlistEntry
        {
            Phone = phone,
            FullName = form.FullName?.Trim(),
            RouteId = form.RouteId,
            RouteFrom = form.RouteFrom?.Trim() ?? routeFrom,
            RouteTo = form.RouteTo?.Trim() ?? routeTo,
            Source = form.RouteId is null ? "landing" : "routes-page",
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new LandingSubmitResponse(
            entity.Id,
            form.RouteId is null
                ? "تم انضمامك لقائمة الانتظار بنجاح"
                : "تم تسجيلك في قائمة انتظار المسار بنجاح");
    }

    public async Task<LandingSubmitResponse> Handle(
        SubmitLandingCaptainLeadCommand request,
        CancellationToken cancellationToken)
    {
        ValidatePhone(request.Request.Phone);

        var phone = PhoneNormalizer.Normalize(request.Request.Phone);
        var entity = new LandingCaptainLead
        {
            Phone = phone,
            FullName = request.Request.FullName?.Trim(),
            VehicleType = request.Request.VehicleType?.Trim(),
            Notes = request.Request.Notes?.Trim(),
        };

        _db.Add(entity);

        // يظهر في شاشة الكباتن مباشرة (طلب بانتظار التفعيل التشغيلي).
        await DriverProvisioning.EnsureDriverAsync(
            _db,
            phone,
            entity.FullName,
            isActive: true,
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new LandingSubmitResponse(
            entity.Id,
            "تم استلام طلب التسجيل ككابتن. سنتواصل معك قريباً");
    }

    private static void ValidatePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 11)
            throw new AppException("يرجى إدخال رقم موبايل صحيح (11 رقم على الأقل)");
    }

    private static string NormalizePhone(string phone) =>
        new string(phone.Where(char.IsDigit).ToArray());
}
