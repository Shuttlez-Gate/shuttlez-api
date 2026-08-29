using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminRideFareRuleDto(
    Guid Id,
    string Name,
    string? FromZoneKey,
    string? ToZoneKey,
    decimal FlatFare,
    decimal BaseFare,
    decimal PricePerKm,
    decimal? MinimumFare,
    decimal? MaximumFare,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record SaveRideFareRuleRequest(
    string Name,
    string? FromZoneKey,
    string? ToZoneKey,
    decimal FlatFare,
    decimal BaseFare = 0,
    decimal PricePerKm = 0,
    decimal? MinimumFare = null,
    decimal? MaximumFare = null,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null,
    bool IsActive = true);

public record AdminRideFareListQuery(bool? ActiveOnly = null)
    : IRequest<IReadOnlyList<AdminRideFareRuleDto>>;

public record SaveRideFareRuleCommand(Guid? Id, SaveRideFareRuleRequest Request)
    : IRequest<AdminRideFareRuleDto>;

public record DeleteRideFareRuleCommand(Guid Id) : IRequest<bool>;

public class AdminRideFareHandlers :
    IRequestHandler<AdminRideFareListQuery, IReadOnlyList<AdminRideFareRuleDto>>,
    IRequestHandler<SaveRideFareRuleCommand, AdminRideFareRuleDto>,
    IRequestHandler<DeleteRideFareRuleCommand, bool>
{
    private readonly IAppDbContext _db;

    public AdminRideFareHandlers(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminRideFareRuleDto>> Handle(
        AdminRideFareListQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.RideFareRules.AsNoTracking().Where(r => !r.IsDeleted);
        if (request.ActiveOnly == true)
            query = query.Where(r => r.IsActive);

        var rows = await query
            .OrderByDescending(r => r.IsActive)
            .ThenByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<AdminRideFareRuleDto> Handle(
        SaveRideFareRuleCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        ValidateRideFareRule(body);

        RideFareRule rule;
        if (request.Id is null)
        {
            rule = new RideFareRule();
            _db.Add(rule);
        }
        else
        {
            rule = await _db.RideFareRules
                .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("قاعدة أجرة المشوار غير موجودة", ErrorCodes.RideFareNotConfigured);
        }

        rule.Name = string.IsNullOrWhiteSpace(body.Name) ? "Default" : body.Name.Trim();
        rule.FromZoneKey = NormalizeZone(body.FromZoneKey);
        rule.ToZoneKey = NormalizeZone(body.ToZoneKey);
        rule.FlatFare = body.FlatFare;
        rule.BaseFare = body.BaseFare;
        rule.PricePerKm = body.PricePerKm;
        rule.MinimumFare = body.MinimumFare;
        rule.MaximumFare = body.MaximumFare;
        rule.EffectiveFrom = body.EffectiveFrom;
        rule.EffectiveTo = body.EffectiveTo;
        rule.IsActive = body.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        if (request.Id is not null)
            _db.Update(rule);

        await _db.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public async Task<bool> Handle(
        DeleteRideFareRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await _db.RideFareRules
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("قاعدة أجرة المشوار غير موجودة", ErrorCodes.RideFareNotConfigured);

        rule.IsDeleted = true;
        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        _db.Update(rule);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateRideFareRule(SaveRideFareRuleRequest body)
    {
        if (body.PricePerKm > 0)
            return;

        if (body.FlatFare <= 0)
            throw new AppException("أجرة المشوار الثابتة يجب أن تكون أكبر من صفر عندما لا يُستخدم السعر لكل كيلومتر");
    }

    private static AdminRideFareRuleDto Map(RideFareRule r) => new(
        r.Id,
        r.Name,
        r.FromZoneKey,
        r.ToZoneKey,
        r.FlatFare,
        r.BaseFare,
        r.PricePerKm,
        r.MinimumFare,
        r.MaximumFare,
        r.EffectiveFrom,
        r.EffectiveTo,
        r.IsActive,
        r.CreatedAt,
        r.UpdatedAt);

    private static string? NormalizeZone(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : key.Trim();
}
