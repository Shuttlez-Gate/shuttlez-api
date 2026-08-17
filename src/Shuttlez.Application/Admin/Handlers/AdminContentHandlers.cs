using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminFaqListQuery : IRequest<IReadOnlyList<AdminFaqDto>>;
public record SaveFaqCommand(Guid? Id, SaveFaqRequest Request) : IRequest<AdminFaqDto>;
public record DeleteFaqCommand(Guid Id) : IRequest<bool>;

public record AdminLegalListQuery : IRequest<IReadOnlyList<AdminLegalDto>>;
public record SaveLegalCommand(Guid? Id, SaveLegalRequest Request) : IRequest<AdminLegalDto>;
public record DeleteLegalCommand(Guid Id) : IRequest<bool>;

public record AdminPackageListQuery : IRequest<IReadOnlyList<AdminPackageDto>>;
public record SavePackageCommand(Guid? Id, SavePackageRequest Request) : IRequest<AdminPackageDto>;
public record DeletePackageCommand(Guid Id) : IRequest<bool>;

public class AdminContentHandlers :
    IRequestHandler<AdminFaqListQuery, IReadOnlyList<AdminFaqDto>>,
    IRequestHandler<SaveFaqCommand, AdminFaqDto>,
    IRequestHandler<DeleteFaqCommand, bool>,
    IRequestHandler<AdminLegalListQuery, IReadOnlyList<AdminLegalDto>>,
    IRequestHandler<SaveLegalCommand, AdminLegalDto>,
    IRequestHandler<DeleteLegalCommand, bool>,
    IRequestHandler<AdminPackageListQuery, IReadOnlyList<AdminPackageDto>>,
    IRequestHandler<SavePackageCommand, AdminPackageDto>,
    IRequestHandler<DeletePackageCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminContentHandlers(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    // ── FAQ ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AdminFaqDto>> Handle(
        AdminFaqListQuery request,
        CancellationToken cancellationToken) =>
        await _db.FaqItems
            .Where(f => !f.IsDeleted)
            .OrderBy(f => f.Order)
            .Select(f => new AdminFaqDto(f.Id, f.Question, f.Answer, f.Order, f.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<AdminFaqDto> Handle(
        SaveFaqCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        if (string.IsNullOrWhiteSpace(body.Question) || string.IsNullOrWhiteSpace(body.Answer))
        {
            throw new AppException("السؤال والجواب مطلوبان");
        }

        FaqItem item;
        if (request.Id is null)
        {
            item = new FaqItem();
            _db.Add(item);
        }
        else
        {
            item = await _db.FaqItems
                .FirstOrDefaultAsync(f => f.Id == request.Id && !f.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("السؤال غير موجود");
            item.UpdatedAt = _clock.UtcNow;
            _db.Update(item);
        }

        item.Question = body.Question.Trim();
        item.Answer = body.Answer.Trim();
        item.Order = body.Order;
        item.IsActive = body.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return item.ToDto();
    }

    public async Task<bool> Handle(
        DeleteFaqCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _db.FaqItems
            .FirstOrDefaultAsync(f => f.Id == request.Id && !f.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("السؤال غير موجود");

        item.IsDeleted = true;
        item.IsActive = false;
        item.UpdatedAt = _clock.UtcNow;
        _db.Update(item);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ── Legal documents ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AdminLegalDto>> Handle(
        AdminLegalListQuery request,
        CancellationToken cancellationToken) =>
        await _db.LegalDocuments
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Slug)
            .Select(d => new AdminLegalDto(d.Id, d.Slug, d.Title, d.Content, d.IsActive, d.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<AdminLegalDto> Handle(
        SaveLegalCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        var slug = body.Slug?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new AppException("المعرّف (slug) مطلوب");
        }
        if (string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Content))
        {
            throw new AppException("العنوان والمحتوى مطلوبان");
        }

        var duplicate = await _db.LegalDocuments
            .AnyAsync(d => d.Slug == slug && !d.IsDeleted && d.Id != request.Id, cancellationToken);
        if (duplicate)
        {
            throw new AppException("هذا المعرّف مستخدم بالفعل");
        }

        LegalDocument doc;
        if (request.Id is null)
        {
            doc = new LegalDocument();
            _db.Add(doc);
        }
        else
        {
            doc = await _db.LegalDocuments
                .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("المستند غير موجود");
            _db.Update(doc);
        }

        doc.Slug = slug;
        doc.Title = body.Title.Trim();
        doc.Content = body.Content;
        doc.IsActive = body.IsActive;
        doc.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return doc.ToDto();
    }

    public async Task<bool> Handle(
        DeleteLegalCommand request,
        CancellationToken cancellationToken)
    {
        var doc = await _db.LegalDocuments
            .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("المستند غير موجود");

        doc.IsDeleted = true;
        doc.IsActive = false;
        doc.UpdatedAt = _clock.UtcNow;
        _db.Update(doc);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ── Subscription packages ───────────────────────────────────────────────

    public async Task<IReadOnlyList<AdminPackageDto>> Handle(
        AdminPackageListQuery request,
        CancellationToken cancellationToken) =>
        await _db.SubscriptionPackages
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Price)
            .Select(p => new AdminPackageDto(
                p.Id, p.Name, p.Description, p.Price, p.TripCount, p.ValidityDays, p.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<AdminPackageDto> Handle(
        SavePackageCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            throw new AppException("اسم الباقة مطلوب");
        }
        if (body.Price < 0)
        {
            throw new AppException("السعر لا يمكن أن يكون سالباً");
        }
        if (body.TripCount < 1 || body.ValidityDays < 1)
        {
            throw new AppException("عدد الرحلات ومدة الصلاحية يجب أن يكونا 1 على الأقل");
        }

        SubscriptionPackage package;
        if (request.Id is null)
        {
            package = new SubscriptionPackage();
            _db.Add(package);
        }
        else
        {
            package = await _db.SubscriptionPackages
                .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("الباقة غير موجودة");
            package.UpdatedAt = _clock.UtcNow;
            _db.Update(package);
        }

        package.Name = body.Name.Trim();
        package.Description = body.Description?.Trim();
        package.Price = body.Price;
        package.TripCount = body.TripCount;
        package.ValidityDays = body.ValidityDays;
        package.IsActive = body.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return package.ToDto();
    }

    public async Task<bool> Handle(
        DeletePackageCommand request,
        CancellationToken cancellationToken)
    {
        var package = await _db.SubscriptionPackages
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الباقة غير موجودة");

        package.IsDeleted = true;
        package.IsActive = false;
        package.UpdatedAt = _clock.UtcNow;
        _db.Update(package);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
