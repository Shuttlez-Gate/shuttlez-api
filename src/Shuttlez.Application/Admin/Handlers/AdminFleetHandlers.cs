using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Handlers;

// ── Vehicles ────────────────────────────────────────────────────────────────

public record AdminVehiclesQuery(
    string? Search = null,
    string? Type = null,
    bool? IsActive = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminVehicleDto>>;

public record SaveVehicleCommand(Guid? Id, SaveVehicleRequest Request) : IRequest<AdminVehicleDto>;

public record DeleteVehicleCommand(Guid Id) : IRequest<bool>;

// ── Drivers ─────────────────────────────────────────────────────────────────

public record AdminDriversQuery(
    string? Search = null,
    bool? IsOnline = null,
    bool? IsActive = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminDriverDto>>;

public record CreateDriverCommand(CreateDriverRequest Request) : IRequest<AdminDriverDto>;

public record UpdateDriverCommand(Guid Id, UpdateDriverRequest Request) : IRequest<AdminDriverDto>;

public record DeleteDriverCommand(Guid Id) : IRequest<bool>;

public class AdminFleetHandlers :
    IRequestHandler<AdminVehiclesQuery, PagedResult<AdminVehicleDto>>,
    IRequestHandler<SaveVehicleCommand, AdminVehicleDto>,
    IRequestHandler<DeleteVehicleCommand, bool>,
    IRequestHandler<AdminDriversQuery, PagedResult<AdminDriverDto>>,
    IRequestHandler<CreateDriverCommand, AdminDriverDto>,
    IRequestHandler<UpdateDriverCommand, AdminDriverDto>,
    IRequestHandler<DeleteDriverCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminFleetHandlers(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<AdminVehicleDto>> Handle(
        AdminVehiclesQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.Vehicles.Where(v => !v.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(v => v.PlateNumber.Contains(term) || v.Model.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = AdminMapper.ParseVehicleType(request.Type);
            query = query.Where(v => v.Type == type);
        }

        if (request.IsActive is not null)
        {
            query = query.Where(v => v.IsActive == request.IsActive);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(VehicleProjection())
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminVehicleDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<AdminVehicleDto> Handle(
        SaveVehicleCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        if (string.IsNullOrWhiteSpace(body.PlateNumber))
        {
            throw new AppException("رقم اللوحة مطلوب");
        }
        if (body.Capacity < 1)
        {
            throw new AppException("عدد المقاعد يجب أن يكون 1 على الأقل");
        }

        Vehicle vehicle;
        if (request.Id is null)
        {
            vehicle = new Vehicle();
            _db.Add(vehicle);
        }
        else
        {
            vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.Id == request.Id && !v.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("المركبة غير موجودة");
            vehicle.UpdatedAt = _clock.UtcNow;
            _db.Update(vehicle);
        }

        vehicle.PlateNumber = body.PlateNumber.Trim();
        vehicle.Model = body.Model?.Trim() ?? string.Empty;
        vehicle.Type = AdminMapper.ParseVehicleType(body.Type);
        vehicle.Capacity = body.Capacity;
        vehicle.IsActive = body.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Vehicles
            .Where(v => v.Id == vehicle.Id)
            .Select(VehicleProjection())
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> Handle(
        DeleteVehicleCommand request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.Id && !v.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("المركبة غير موجودة");

        var assigned = await _db.Drivers
            .AnyAsync(d => d.VehicleId == vehicle.Id && !d.IsDeleted, cancellationToken);
        if (assigned)
        {
            throw new AppException("لا يمكن حذف مركبة مرتبطة بكابتن. افصل الارتباط أولاً");
        }

        vehicle.IsDeleted = true;
        vehicle.IsActive = false;
        vehicle.UpdatedAt = _clock.UtcNow;
        _db.Update(vehicle);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<AdminDriverDto>> Handle(
        AdminDriversQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.Drivers.Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(d =>
                d.User.Phone.Contains(term)
                || (d.User.FullName != null && d.User.FullName.Contains(term)));
        }

        if (request.IsOnline is not null)
        {
            query = query.Where(d => d.IsOnline == request.IsOnline);
        }

        if (request.IsActive is not null)
        {
            query = query.Where(d => d.IsActive == request.IsActive);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(DriverProjection())
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminDriverDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<AdminDriverDto> Handle(
        CreateDriverCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Request;
        var phone = PhoneNormalizer.Normalize(body.Phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new AppException("رقم الهاتف مطلوب");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Phone = phone,
                FullName = body.FullName?.Trim(),
                Email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim(),
                Gender = ParseGender(body.Gender),
                UserType = UserType.Driver,
                IsActive = true
            };
            _db.Add(user);
            _db.Add(new Wallet { UserId = user.Id, Balance = 0 });
        }
        else
        {
            var alreadyDriver = await _db.Drivers
                .AnyAsync(d => d.UserId == user.Id && !d.IsDeleted, cancellationToken);
            if (alreadyDriver)
            {
                throw new AppException("هذا المستخدم كابتن بالفعل");
            }

            user.UserType = UserType.Driver;
            if (!string.IsNullOrWhiteSpace(body.FullName))
            {
                user.FullName = body.FullName.Trim();
            }

            if (body.Email is not null)
            {
                user.Email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim();
            }

            var gender = ParseGender(body.Gender);
            if (gender is not null)
            {
                user.Gender = gender;
            }

            user.UpdatedAt = _clock.UtcNow;
            _db.Update(user);
        }

        await EnsureVehicleExistsAsync(body.VehicleId, cancellationToken);

        var status = DriverVerificationStatus.Approved;
        if (!string.IsNullOrWhiteSpace(body.VerificationStatus)
            && Enum.TryParse<DriverVerificationStatus>(body.VerificationStatus, true, out var parsed))
        {
            status = parsed;
        }

        var isActive = body.IsActive ?? (status == DriverVerificationStatus.Approved);

        var driver = new Driver
        {
            UserId = user.Id,
            VehicleId = body.VehicleId,
            IsActive = isActive,
            IsOnline = body.IsOnline ?? false,
            VerificationStatus = status,
            VerifiedAt = status == DriverVerificationStatus.Approved ? _clock.UtcNow : null,
            NationalId = string.IsNullOrWhiteSpace(body.NationalId) ? null : body.NationalId.Trim(),
            BirthDate = ParseDateOnly(body.BirthDate),
            LicenseNumber = string.IsNullOrWhiteSpace(body.LicenseNumber) ? null : body.LicenseNumber.Trim(),
            LicenseType = string.IsNullOrWhiteSpace(body.LicenseType) ? null : body.LicenseType.Trim(),
            LicenseExpiry = ParseDateOnly(body.LicenseExpiry),
            VehicleKind = string.IsNullOrWhiteSpace(body.VehicleKind) ? null : body.VehicleKind.Trim(),
            VehicleModelName = string.IsNullOrWhiteSpace(body.VehicleModelName) ? null : body.VehicleModelName.Trim(),
            ManufactureYear = body.ManufactureYear is null or 0 ? null : body.ManufactureYear,
            PlateNumber = string.IsNullOrWhiteSpace(body.PlateNumber) ? null : body.PlateNumber.Trim(),
            VehicleColor = string.IsNullOrWhiteSpace(body.VehicleColor) ? null : body.VehicleColor.Trim(),
            Seats = body.Seats is null or 0 ? null : body.Seats,
            AdminNotes = string.IsNullOrWhiteSpace(body.AdminNotes) ? null : body.AdminNotes.Trim()
        };
        _db.Add(driver);
        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Drivers
            .Where(d => d.Id == driver.Id)
            .Select(DriverProjection())
            .FirstAsync(cancellationToken);
    }

    private static Gender? ParseGender(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim() switch
            {
                "أنثى" or "female" or "Female" => Gender.Female,
                "ذكر" or "male" or "Male" => Gender.Male,
                _ => null
            };

    public async Task<AdminDriverDto> Handle(
        UpdateDriverCommand request,
        CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الكابتن غير موجود");

        var body = request.Request;

        if (body.VehicleId is not null)
        {
            await EnsureVehicleExistsAsync(body.VehicleId, cancellationToken);
            driver.VehicleId = body.VehicleId == Guid.Empty ? null : body.VehicleId;
        }

        if (body.IsOnline is not null) driver.IsOnline = body.IsOnline.Value;
        if (body.IsActive is not null) driver.IsActive = body.IsActive.Value;

        if (!string.IsNullOrWhiteSpace(body.FullName))
        {
            driver.User.FullName = body.FullName.Trim();
            driver.User.UpdatedAt = _clock.UtcNow;
        }

        if (body.Email is not null)
        {
            driver.User.Email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim();
            driver.User.UpdatedAt = _clock.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(body.Gender))
        {
            driver.User.Gender = body.Gender.Trim() switch
            {
                "أنثى" or "female" or "Female" => Gender.Female,
                "ذكر" or "male" or "Male" => Gender.Male,
                _ => driver.User.Gender
            };
            driver.User.UpdatedAt = _clock.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(body.VerificationStatus)
            && Enum.TryParse<DriverVerificationStatus>(body.VerificationStatus, true, out var status))
        {
            driver.VerificationStatus = status;
            if (status == DriverVerificationStatus.Approved)
            {
                driver.VerifiedAt = _clock.UtcNow;
                if (body.IsActive is null)
                {
                    driver.IsActive = true;
                }
            }
            else if (status == DriverVerificationStatus.Rejected && body.IsActive is null)
            {
                driver.IsActive = false;
            }
        }

        if (body.NationalId is not null)
            driver.NationalId = string.IsNullOrWhiteSpace(body.NationalId) ? null : body.NationalId.Trim();

        if (body.BirthDate is not null)
            driver.BirthDate = ParseDateOnly(body.BirthDate);

        if (body.LicenseNumber is not null)
            driver.LicenseNumber = string.IsNullOrWhiteSpace(body.LicenseNumber) ? null : body.LicenseNumber.Trim();

        if (body.LicenseType is not null)
            driver.LicenseType = string.IsNullOrWhiteSpace(body.LicenseType) ? null : body.LicenseType.Trim();

        if (body.LicenseExpiry is not null)
            driver.LicenseExpiry = ParseDateOnly(body.LicenseExpiry);

        if (body.VehicleKind is not null)
            driver.VehicleKind = string.IsNullOrWhiteSpace(body.VehicleKind) ? null : body.VehicleKind.Trim();

        if (body.VehicleModelName is not null)
            driver.VehicleModelName = string.IsNullOrWhiteSpace(body.VehicleModelName) ? null : body.VehicleModelName.Trim();

        if (body.ManufactureYear is not null)
            driver.ManufactureYear = body.ManufactureYear == 0 ? null : body.ManufactureYear;

        if (body.PlateNumber is not null)
            driver.PlateNumber = string.IsNullOrWhiteSpace(body.PlateNumber) ? null : body.PlateNumber.Trim();

        if (body.VehicleColor is not null)
            driver.VehicleColor = string.IsNullOrWhiteSpace(body.VehicleColor) ? null : body.VehicleColor.Trim();

        if (body.Seats is not null)
            driver.Seats = body.Seats == 0 ? null : body.Seats;

        if (body.AdminNotes is not null)
            driver.AdminNotes = string.IsNullOrWhiteSpace(body.AdminNotes) ? null : body.AdminNotes.Trim();

        driver.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Drivers
            .Where(d => d.Id == driver.Id)
            .Select(DriverProjection())
            .FirstAsync(cancellationToken);
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateOnly.TryParse(value.Trim(), out var d) ? d : null;
    }

    public async Task<bool> Handle(
        DeleteDriverCommand request,
        CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الكابتن غير موجود");

        var hasActiveTrips = await _db.Trips.AnyAsync(
            t => t.DriverId == driver.Id
                && !t.IsDeleted
                && (t.Status == TripStatus.Scheduled
                    || t.Status == TripStatus.DriverAssigned
                    || t.Status == TripStatus.InProgress),
            cancellationToken);

        if (hasActiveTrips)
        {
            throw new AppException("لا يمكن حذف كابتن مرتبط برحلات قادمة أو جارية");
        }

        driver.IsDeleted = true;
        driver.IsActive = false;
        driver.IsOnline = false;
        driver.UpdatedAt = _clock.UtcNow;
        _db.Update(driver);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureVehicleExistsAsync(Guid? vehicleId, CancellationToken ct)
    {
        if (vehicleId is null || vehicleId == Guid.Empty) return;

        var exists = await _db.Vehicles.AnyAsync(v => v.Id == vehicleId && !v.IsDeleted, ct);
        if (!exists)
        {
            throw new NotFoundException("المركبة غير موجودة");
        }
    }

    private System.Linq.Expressions.Expression<Func<Vehicle, AdminVehicleDto>> VehicleProjection() =>
        v => new AdminVehicleDto(
            v.Id,
            v.PlateNumber,
            v.Model,
            v.Type == VehicleType.Bus
                ? "bus"
                : v.Type == VehicleType.MiniBus ? "minibus" : "carshuttle",
            v.Capacity,
            v.IsActive,
            _db.Drivers.Count(d => d.VehicleId == v.Id && !d.IsDeleted),
            v.CreatedAt);

    private System.Linq.Expressions.Expression<Func<Driver, AdminDriverDto>> DriverProjection() =>
        d => new AdminDriverDto(
            d.Id,
            d.UserId,
            d.User.Phone,
            d.User.FullName,
            d.VehicleId,
            d.Vehicle == null ? null : d.Vehicle.PlateNumber,
            d.Vehicle == null ? null : d.Vehicle.Model,
            d.RatingAverage,
            d.RatingCount,
            d.IsOnline,
            d.IsActive,
            d.VerificationStatus == DriverVerificationStatus.Pending
                ? "pending"
                : d.VerificationStatus == DriverVerificationStatus.Rejected
                    ? "rejected"
                    : "approved",
            _db.DriverDocuments.Count(doc => doc.DriverId == d.Id && !doc.IsDeleted),
            _db.Trips.Count(t => t.DriverId == d.Id && !t.IsDeleted),
            d.CreatedAt);
}
