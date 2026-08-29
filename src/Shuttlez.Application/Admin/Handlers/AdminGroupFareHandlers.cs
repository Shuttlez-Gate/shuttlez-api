using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminGroupFareRuleDto(
    Guid Id,
    string Name,
    string? FromZoneKey,
    string? ToZoneKey,
    decimal CharterFlatFare,
    decimal BaseFare,
    decimal PricePerKm,
    decimal? MinimumFare,
    decimal? MaximumFare,
    int MaxMembers,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record SaveGroupFareRuleRequest(
    string Name,
    string? FromZoneKey,
    string? ToZoneKey,
    decimal CharterFlatFare,
    int MaxMembers,
    decimal BaseFare = 0,
    decimal PricePerKm = 0,
    decimal? MinimumFare = null,
    decimal? MaximumFare = null,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null,
    bool IsActive = true);

public record AdminGroupFareListQuery(bool? ActiveOnly = null)
    : IRequest<IReadOnlyList<AdminGroupFareRuleDto>>;

public record SaveGroupFareRuleCommand(Guid? Id, SaveGroupFareRuleRequest Request)
    : IRequest<AdminGroupFareRuleDto>;

public record DeleteGroupFareRuleCommand(Guid Id) : IRequest<bool>;

public class AdminGroupFareHandlers :
    IRequestHandler<AdminGroupFareListQuery, IReadOnlyList<AdminGroupFareRuleDto>>,
    IRequestHandler<SaveGroupFareRuleCommand, AdminGroupFareRuleDto>,
    IRequestHandler<DeleteGroupFareRuleCommand, bool>
{
    private readonly IAppDbContext _db;

    public AdminGroupFareHandlers(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminGroupFareRuleDto>> Handle(
        AdminGroupFareListQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.GroupFareRules.AsNoTracking().Where(r => !r.IsDeleted);
        if (request.ActiveOnly == true)
            query = query.Where(r => r.IsActive);

        var rows = await query
            .OrderByDescending(r => r.IsActive)
            .ThenByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<AdminGroupFareRuleDto> Handle(
        SaveGroupFareRuleCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        ValidateGroupFareRule(body);

        if (body.MaxMembers < 1)
            throw new AppException("الحد الأقصى للأعضاء يجب أن يكون 1 على الأقل", 400, ErrorCodes.GroupInvalidCapacity);

        GroupFareRule rule;
        if (request.Id is null)
        {
            rule = new GroupFareRule();
            _db.Add(rule);
        }
        else
        {
            rule = await _db.GroupFareRules
                .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("قاعدة أجرة المجموعة غير موجودة", ErrorCodes.GroupFareNotConfigured);
        }

        rule.Name = string.IsNullOrWhiteSpace(body.Name) ? "Default" : body.Name.Trim();
        rule.FromZoneKey = NormalizeZone(body.FromZoneKey);
        rule.ToZoneKey = NormalizeZone(body.ToZoneKey);
        rule.CharterFlatFare = body.CharterFlatFare;
        rule.BaseFare = body.BaseFare;
        rule.PricePerKm = body.PricePerKm;
        rule.MinimumFare = body.MinimumFare;
        rule.MaximumFare = body.MaximumFare;
        rule.MaxMembers = body.MaxMembers;
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
        DeleteGroupFareRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await _db.GroupFareRules
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("قاعدة أجرة المجموعة غير موجودة", ErrorCodes.GroupFareNotConfigured);

        rule.IsDeleted = true;
        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        _db.Update(rule);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateGroupFareRule(SaveGroupFareRuleRequest body)
    {
        if (body.PricePerKm > 0)
            return;

        if (body.CharterFlatFare <= 0)
            throw new AppException("أجرة المجموعة الثابتة يجب أن تكون أكبر من صفر عندما لا يُستخدم السعر لكل كيلومتر");
    }

    private static AdminGroupFareRuleDto Map(GroupFareRule r) => new(
        r.Id,
        r.Name,
        r.FromZoneKey,
        r.ToZoneKey,
        r.CharterFlatFare,
        r.BaseFare,
        r.PricePerKm,
        r.MinimumFare,
        r.MaximumFare,
        r.MaxMembers,
        r.EffectiveFrom,
        r.EffectiveTo,
        r.IsActive,
        r.CreatedAt,
        r.UpdatedAt);

    private static string? NormalizeZone(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : key.Trim();
}
