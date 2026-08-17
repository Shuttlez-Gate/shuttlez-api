using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminRouteRequestsQuery(
    string? Search = null,
    string? Status = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminRouteRequestDto>>;

public record UpdateRouteRequestStatusCommand(Guid Id, UpdateRouteRequestStatusRequest Request)
    : IRequest<AdminRouteRequestDto>;

public record DeleteRouteRequestCommand(Guid Id) : IRequest<bool>;

/// <summary>يجمع الثلاثة أنواع من ليدز صفحة الهبوط في قائمة واحدة: route / waitlist / captain.</summary>
public record AdminLandingLeadsQuery(
    string? Kind = null,
    string? Search = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminLandingLeadDto>>;

public class AdminRequestHandlers :
    IRequestHandler<AdminRouteRequestsQuery, PagedResult<AdminRouteRequestDto>>,
    IRequestHandler<UpdateRouteRequestStatusCommand, AdminRouteRequestDto>,
    IRequestHandler<DeleteRouteRequestCommand, bool>,
    IRequestHandler<AdminLandingLeadsQuery, PagedResult<AdminLandingLeadDto>>
{
    private static readonly string[] AllowedStatuses =
        ["pending", "approved", "rejected", "converted"];

    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminRequestHandlers(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<AdminRouteRequestDto>> Handle(
        AdminRouteRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.RouteRequests.Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToLowerInvariant();
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(r =>
                r.FromAddress.Contains(term)
                || r.ToAddress.Contains(term)
                || r.User.Phone.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                Phone = r.User.Phone,
                Name = r.User.FullName,
                r.FromAddress,
                r.ToAddress,
                r.Status,
                r.Notes,
                r.PreferredVehicleType,
                r.FromLatitude,
                r.FromLongitude,
                r.ToLatitude,
                r.ToLongitude,
                r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r =>
        {
            var notes = AdminMapper.ParseRouteRequestNotes(r.Notes);
            return new AdminRouteRequestDto(
                r.Id,
                r.UserId,
                r.Phone,
                r.Name,
                r.FromAddress,
                r.ToAddress,
                r.Status,
                notes.FromTime,
                notes.ToTime,
                notes.WeeklyCount,
                notes.UsageDays,
                notes.UsageReason,
                string.IsNullOrWhiteSpace(r.PreferredVehicleType) ? "minibus" : r.PreferredVehicleType,
                r.FromLatitude,
                r.FromLongitude,
                r.ToLatitude,
                r.ToLongitude,
                r.CreatedAt);
        }).ToList();

        return new PagedResult<AdminRouteRequestDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<AdminRouteRequestDto> Handle(
        UpdateRouteRequestStatusCommand request,
        CancellationToken cancellationToken)
    {
        var status = request.Request.Status?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status))
        {
            throw new AppException(
                "حالة غير مسموحة. المسموح: " + string.Join(" / ", AllowedStatuses));
        }

        var entity = await _db.RouteRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الطلب غير موجود");

        entity.Status = status;

        if (!string.IsNullOrWhiteSpace(request.Request.AdminNote))
        {
            var notes = AdminMapper.ParseRouteRequestNotes(entity.Notes);
            entity.Notes = AdminMapper.SerializeRouteRequestNotes(
                notes with { AdminNote = request.Request.AdminNote.Trim() });
        }

        entity.UpdatedAt = _clock.UtcNow;

        _db.Add(new Notification
        {
            UserId = entity.UserId,
            Title = "تحديث طلب خط السير",
            Body = status switch
            {
                "approved" => "تمت الموافقة على طلبك، وسنبدأ تشغيل الخط قريباً",
                "rejected" => "لم نتمكن من تنفيذ طلبك حالياً. شكراً لتواصلك معنا",
                "converted" => "خط السير الذي طلبته صار متاحاً للحجز",
                _ => "طلبك قيد المراجعة"
            },
            Type = "route_request"
        });

        await _db.SaveChangesAsync(cancellationToken);

        var parsed = AdminMapper.ParseRouteRequestNotes(entity.Notes);
        return new AdminRouteRequestDto(
            entity.Id,
            entity.UserId,
            entity.User.Phone,
            entity.User.FullName,
            entity.FromAddress,
            entity.ToAddress,
            entity.Status,
            parsed.FromTime,
            parsed.ToTime,
            parsed.WeeklyCount,
            parsed.UsageDays,
            parsed.UsageReason,
            string.IsNullOrWhiteSpace(entity.PreferredVehicleType) ? "minibus" : entity.PreferredVehicleType,
            entity.FromLatitude,
            entity.FromLongitude,
            entity.ToLatitude,
            entity.ToLongitude,
            entity.CreatedAt);
    }

    public async Task<bool> Handle(
        DeleteRouteRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.RouteRequests
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الطلب غير موجود");

        entity.IsDeleted = true;
        entity.UpdatedAt = _clock.UtcNow;
        _db.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<AdminLandingLeadDto>> Handle(
        AdminLandingLeadsQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var kind = request.Kind?.Trim().ToLowerInvariant();
        var term = request.Search?.Trim();

        var leads = new List<AdminLandingLeadDto>();

        if (kind is null or "" or "all" or "route")
        {
            var rows = await _db.LandingRouteLeads
                .Where(l => !l.IsDeleted)
                .Where(l => term == null || l.Phone.Contains(term)
                    || l.FromCity.Contains(term) || l.ToCity.Contains(term))
                .ToListAsync(cancellationToken);

            leads.AddRange(rows.Select(l => new AdminLandingLeadDto(
                l.Id, "route", l.Phone, null,
                $"{l.FromRegion}، {l.FromCity}",
                $"{l.ToRegion}، {l.ToCity}",
                l.FromTime, l.ToTime, l.WeeklyCount, l.UsageDays, l.UsageReason,
                null, null, l.Source, l.CreatedAt)));
        }

        if (kind is null or "" or "all" or "waitlist")
        {
            var rows = await _db.LandingWaitlistEntries
                .Where(l => !l.IsDeleted)
                .Where(l => term == null || l.Phone.Contains(term)
                    || (l.FullName != null && l.FullName.Contains(term)))
                .ToListAsync(cancellationToken);

            leads.AddRange(rows.Select(l => new AdminLandingLeadDto(
                l.Id, "waitlist", l.Phone, l.FullName,
                l.RouteFrom, l.RouteTo,
                null, null, null, null, null,
                null, null, l.Source, l.CreatedAt)));
        }

        if (kind is null or "" or "all" or "captain")
        {
            var rows = await _db.LandingCaptainLeads
                .Where(l => !l.IsDeleted)
                .Where(l => term == null || l.Phone.Contains(term)
                    || (l.FullName != null && l.FullName.Contains(term)))
                .ToListAsync(cancellationToken);

            leads.AddRange(rows.Select(l => new AdminLandingLeadDto(
                l.Id, "captain", l.Phone, l.FullName,
                null, null, null, null, null, null, null,
                l.VehicleType, l.Notes, l.Source, l.CreatedAt)));
        }

        var ordered = leads.OrderByDescending(l => l.CreatedAt).ToList();
        var items = ordered.Skip(page.Skip).Take(page.PageSize).ToList();

        return new PagedResult<AdminLandingLeadDto>(items, page.Page, page.PageSize, ordered.Count);
    }
}
