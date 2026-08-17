using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminDriverDetailsQuery(Guid Id) : IRequest<AdminDriverDetailsDto>;

public record UploadDriverDocumentCommand(
    Guid DriverId,
    DriverDocumentType DocumentType,
    Stream Content,
    string FileName,
    string ContentType,
    string UploadedBy = "admin",
    string? Notes = null) : IRequest<AdminDriverDocumentDto>;

public record DeleteDriverDocumentCommand(Guid DriverId, Guid DocumentId) : IRequest<bool>;

public class AdminDriverKycHandlers :
    IRequestHandler<AdminDriverDetailsQuery, AdminDriverDetailsDto>,
    IRequestHandler<UploadDriverDocumentCommand, AdminDriverDocumentDto>,
    IRequestHandler<DeleteDriverDocumentCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _files;
    private readonly IDateTimeProvider _clock;

    public AdminDriverKycHandlers(
        IAppDbContext db,
        IFileStorageService files,
        IDateTimeProvider clock)
    {
        _db = db;
        _files = files;
        _clock = clock;
    }

    public async Task<AdminDriverDetailsDto> Handle(
        AdminDriverDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .Include(d => d.User)
            .Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الكابتن غير موجود");

        var docs = await _db.DriverDocuments
            .Where(d => d.DriverId == driver.Id && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        var tripCount = await _db.Trips.CountAsync(
            t => t.DriverId == driver.Id && !t.IsDeleted, cancellationToken);

        return MapDetails(driver, docs, tripCount);
    }

    public async Task<AdminDriverDocumentDto> Handle(
        UploadDriverDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.Id == request.DriverId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الكابتن غير موجود");

        if (request.Content.CanSeek)
        {
            if (request.Content.Length <= 0)
            {
                throw new AppException("الملف فارغ");
            }

            if (request.Content.Length > 15 * 1024 * 1024)
            {
                throw new AppException("حجم الملف يجب ألا يتجاوز 15 ميجابايت");
            }
        }

        var stored = await _files.SaveAsync(
            request.Content,
            request.FileName,
            string.IsNullOrWhiteSpace(request.ContentType)
                ? "application/octet-stream"
                : request.ContentType,
            $"drivers/{driver.Id:N}",
            cancellationToken);

        if (stored.SizeBytes <= 0)
        {
            await _files.DeleteAsync(stored.StoragePath, cancellationToken);
            throw new AppException("الملف فارغ");
        }

        if (stored.SizeBytes > 15 * 1024 * 1024)
        {
            await _files.DeleteAsync(stored.StoragePath, cancellationToken);
            throw new AppException("حجم الملف يجب ألا يتجاوز 15 ميجابايت");
        }

        var doc = new DriverDocument
        {
            DriverId = driver.Id,
            DocumentType = request.DocumentType,
            FileName = stored.FileName,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            StoragePath = stored.StoragePath,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            UploadedBy = string.IsNullOrWhiteSpace(request.UploadedBy) ? "admin" : request.UploadedBy
        };
        _db.Add(doc);
        await _db.SaveChangesAsync(cancellationToken);

        return MapDocument(doc);
    }

    public async Task<bool> Handle(
        DeleteDriverDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var doc = await _db.DriverDocuments
            .FirstOrDefaultAsync(
                d => d.Id == request.DocumentId
                    && d.DriverId == request.DriverId
                    && !d.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("المرفق غير موجود");

        doc.IsDeleted = true;
        doc.UpdatedAt = _clock.UtcNow;
        _db.Update(doc);
        await _db.SaveChangesAsync(cancellationToken);
        await _files.DeleteAsync(doc.StoragePath, cancellationToken);
        return true;
    }

    internal static AdminDriverDetailsDto MapDetails(
        Driver driver,
        IReadOnlyList<DriverDocument> docs,
        int tripCount) =>
        new(
            driver.Id,
            driver.UserId,
            driver.User.Phone,
            driver.User.FullName,
            driver.User.Email,
            driver.User.Gender?.ToString(),
            driver.User.AvatarUrl,
            driver.VehicleId,
            driver.Vehicle?.PlateNumber,
            driver.Vehicle?.Model,
            driver.RatingAverage,
            driver.RatingCount,
            driver.IsOnline,
            driver.IsActive,
            driver.VerificationStatus.ToString().ToLowerInvariant(),
            driver.NationalId,
            driver.BirthDate?.ToString("yyyy-MM-dd"),
            driver.LicenseNumber,
            driver.LicenseType,
            driver.LicenseExpiry?.ToString("yyyy-MM-dd"),
            driver.VehicleKind,
            driver.VehicleModelName,
            driver.ManufactureYear,
            driver.PlateNumber,
            driver.VehicleColor,
            driver.Seats,
            driver.AdminNotes,
            driver.VerifiedAt,
            tripCount,
            driver.CreatedAt,
            docs.Select(MapDocument).ToList());

    internal static AdminDriverDocumentDto MapDocument(DriverDocument doc) =>
        new(
            doc.Id,
            doc.DocumentType.ToString(),
            doc.FileName,
            doc.ContentType,
            doc.SizeBytes,
            doc.StoragePath,
            doc.UploadedBy,
            doc.Notes,
            doc.CreatedAt);
}
