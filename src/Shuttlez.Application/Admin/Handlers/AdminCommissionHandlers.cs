using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminCommissionRuleDto(
    Guid Id,
    string Name,
    decimal PlatformCommissionPercent,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive);

public record SaveCommissionRuleRequest(
    string Name,
    decimal PlatformCommissionPercent,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive = true);

public record AdminCommissionListQuery : IRequest<IReadOnlyList<AdminCommissionRuleDto>>;

public record SaveCommissionRuleCommand(Guid? Id, SaveCommissionRuleRequest Request)
    : IRequest<AdminCommissionRuleDto>;

public class AdminCommissionHandlers :
    IRequestHandler<AdminCommissionListQuery, IReadOnlyList<AdminCommissionRuleDto>>,
    IRequestHandler<SaveCommissionRuleCommand, AdminCommissionRuleDto>
{
    private readonly IAppDbContext _db;

    public AdminCommissionHandlers(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminCommissionRuleDto>> Handle(
        AdminCommissionListQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.CommissionRules
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.IsActive)
            .ThenByDescending(r => r.EffectiveFrom)
            .Select(r => new AdminCommissionRuleDto(
                r.Id,
                r.Name,
                r.PlatformCommissionPercent,
                r.EffectiveFrom,
                r.EffectiveTo,
                r.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminCommissionRuleDto> Handle(
        SaveCommissionRuleCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        if (body.PlatformCommissionPercent is < 0 or > 100)
            throw new AppException("نسبة العمولة يجب أن تكون بين 0 و 100");

        CommissionRule rule;
        if (request.Id is null)
        {
            rule = new CommissionRule();
            _db.Add(rule);
        }
        else
        {
            rule = await _db.CommissionRules
                .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("قاعدة العمولة غير موجودة");
        }

        rule.Name = string.IsNullOrWhiteSpace(body.Name) ? "Default" : body.Name.Trim();
        rule.PlatformCommissionPercent = body.PlatformCommissionPercent;
        rule.EffectiveFrom = body.EffectiveFrom;
        rule.EffectiveTo = body.EffectiveTo;
        rule.IsActive = body.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        if (body.IsActive)
        {
            var others = await _db.CommissionRules
                .Where(r => r.Id != rule.Id && r.IsActive && !r.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
            {
                other.IsActive = false;
                other.UpdatedAt = DateTime.UtcNow;
                _db.Update(other);
            }
        }

        if (request.Id is not null)
            _db.Update(rule);

        await _db.SaveChangesAsync(cancellationToken);

        return new AdminCommissionRuleDto(
            rule.Id,
            rule.Name,
            rule.PlatformCommissionPercent,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.IsActive);
    }
}
