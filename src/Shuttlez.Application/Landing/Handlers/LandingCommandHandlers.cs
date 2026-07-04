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

        var exists = await _db.LandingWaitlistEntries
            .AnyAsync(x => x.Phone == phone, cancellationToken);
        if (exists)
            throw new AppException("رقمك مسجل بالفعل في قائمة الانتظار");

        var entity = new LandingWaitlistEntry
        {
            Phone = phone,
            FullName = request.Request.FullName?.Trim(),
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new LandingSubmitResponse(
            entity.Id,
            "تم انضمامك لقائمة الانتظار بنجاح");
    }

    public async Task<LandingSubmitResponse> Handle(
        SubmitLandingCaptainLeadCommand request,
        CancellationToken cancellationToken)
    {
        ValidatePhone(request.Request.Phone);

        var entity = new LandingCaptainLead
        {
            Phone = NormalizePhone(request.Request.Phone),
            FullName = request.Request.FullName?.Trim(),
            VehicleType = request.Request.VehicleType?.Trim(),
            Notes = request.Request.Notes?.Trim(),
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new LandingSubmitResponse(
            entity.Id,
            "تم استلام طلب التسجيل ككابتن. سنتواصل معك قريباً");
    }

    private static void ValidatePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 10)
            throw new AppException("يرجى إدخال رقم موبايل صحيح");
    }

    private static string NormalizePhone(string phone) =>
        new string(phone.Where(char.IsDigit).ToArray());
}
