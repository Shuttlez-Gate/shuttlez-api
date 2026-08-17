using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Handlers;

public record AdminUsersQuery(
    string? Search = null,
    string? UserType = null,
    bool? IsActive = null,
    int? Page = null,
    int? PageSize = null) : IRequest<PagedResult<AdminUserDto>>;

public record AdminUserByIdQuery(Guid Id) : IRequest<AdminUserDto>;

public record CreateAdminUserCommand(CreateAdminUserRequest Request) : IRequest<AdminUserDto>;

public record UpdateAdminUserCommand(Guid Id, UpdateAdminUserRequest Request) : IRequest<AdminUserDto>;

public record DeleteAdminUserCommand(Guid Id) : IRequest<bool>;

public record AdjustWalletCommand(Guid UserId, AdjustWalletRequest Request) : IRequest<decimal>;

public class AdminUserHandlers :
    IRequestHandler<AdminUsersQuery, PagedResult<AdminUserDto>>,
    IRequestHandler<AdminUserByIdQuery, AdminUserDto>,
    IRequestHandler<CreateAdminUserCommand, AdminUserDto>,
    IRequestHandler<UpdateAdminUserCommand, AdminUserDto>,
    IRequestHandler<DeleteAdminUserCommand, bool>,
    IRequestHandler<AdjustWalletCommand, decimal>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminUserHandlers(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<AdminUserDto>> Handle(
        AdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = _db.Users.Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u =>
                u.Phone.Contains(term)
                || (u.FullName != null && u.FullName.Contains(term))
                || (u.Email != null && u.Email.Contains(term)));
        }

        var userType = AdminMapper.ParseUserTypeOrNull(request.UserType);
        if (userType is not null)
        {
            query = query.Where(u => u.UserType == userType);
        }

        if (request.IsActive is not null)
        {
            query = query.Where(u => u.IsActive == request.IsActive);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(Projection())
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<AdminUserDto> Handle(
        AdminUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(u => u.Id == request.Id && !u.IsDeleted)
            .Select(Projection())
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new NotFoundException("المستخدم غير موجود");
    }

    public async Task<AdminUserDto> Handle(
        CreateAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        var phone = PhoneNormalizer.Normalize(request.Request.Phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new AppException("رقم الهاتف مطلوب");
        }

        var exists = await _db.Users.AnyAsync(u => u.Phone == phone && !u.IsDeleted, cancellationToken);
        if (exists)
        {
            throw new AppException("رقم الهاتف مسجّل بالفعل");
        }

        var user = new User
        {
            Phone = phone,
            FullName = request.Request.FullName?.Trim(),
            Email = request.Request.Email?.Trim(),
            Gender = AdminMapper.ParseGender(request.Request.Gender),
            UserType = AdminMapper.ParseUserType(request.Request.UserType),
            IsActive = true
        };

        _db.Add(user);
        _db.Add(new Wallet { UserId = user.Id, Balance = 0 });

        if (user.UserType == UserType.Driver)
        {
            await EnsureDriverProfileAsync(user.Id, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await Handle(new AdminUserByIdQuery(user.Id), cancellationToken);
    }

    public async Task<AdminUserDto> Handle(
        UpdateAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("المستخدم غير موجود");

        var body = request.Request;

        if (body.FullName is not null) user.FullName = body.FullName.Trim();
        if (body.Email is not null) user.Email = body.Email.Trim();
        if (body.AvatarUrl is not null) user.AvatarUrl = body.AvatarUrl.Trim();
        if (body.Gender is not null) user.Gender = AdminMapper.ParseGender(body.Gender);
        if (body.IsActive is not null) user.IsActive = body.IsActive.Value;

        var newType = AdminMapper.ParseUserTypeOrNull(body.UserType);
        if (newType is not null && newType != user.UserType)
        {
            user.UserType = newType.Value;
            if (newType == UserType.Driver)
            {
                await EnsureDriverProfileAsync(user.Id, cancellationToken);
            }
        }

        user.UpdatedAt = _clock.UtcNow;
        _db.Update(user);
        await _db.SaveChangesAsync(cancellationToken);

        return await Handle(new AdminUserByIdQuery(user.Id), cancellationToken);
    }

    public async Task<bool> Handle(
        DeleteAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("المستخدم غير موجود");

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = _clock.UtcNow;
        _db.Update(user);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<decimal> Handle(
        AdjustWalletCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Request.Amount == 0)
        {
            throw new AppException("قيمة التعديل يجب أن تكون مختلفة عن صفر");
        }

        var userExists = await _db.Users
            .AnyAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException("المستخدم غير موجود");
        }

        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.UserId == request.UserId && !w.IsDeleted, cancellationToken);

        if (wallet is null)
        {
            wallet = new Wallet { UserId = request.UserId, Balance = 0 };
            _db.Add(wallet);
        }

        wallet.Balance += request.Request.Amount;
        if (wallet.Balance < 0)
        {
            throw new AppException("الرصيد لا يمكن أن يصبح سالباً");
        }

        wallet.UpdatedAt = _clock.UtcNow;

        _db.Add(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = request.Request.Amount,
            Type = request.Request.Amount > 0 ? "admin_credit" : "admin_debit",
            Description = request.Request.Description ?? "تعديل يدوي من لوحة التحكم"
        });

        await _db.SaveChangesAsync(cancellationToken);
        return wallet.Balance;
    }

    private async Task EnsureDriverProfileAsync(Guid userId, CancellationToken ct)
    {
        var hasDriver = await _db.Drivers.AnyAsync(d => d.UserId == userId && !d.IsDeleted, ct);
        if (!hasDriver)
        {
            _db.Add(new Driver { UserId = userId, IsActive = true });
        }
    }

    private System.Linq.Expressions.Expression<Func<User, AdminUserDto>> Projection() =>
        u => new AdminUserDto(
            u.Id,
            u.Phone,
            u.FullName,
            u.Email,
            u.Gender == Gender.Male ? "male" : u.Gender == Gender.Female ? "female" : null,
            u.AvatarUrl,
            u.UserType == UserType.Admin
                ? "admin"
                : u.UserType == UserType.Driver ? "driver" : "passenger",
            u.RatingAverage,
            u.RatingCount,
            u.IsActive,
            _db.Wallets
                .Where(w => w.UserId == u.Id && !w.IsDeleted)
                .Select(w => w.Balance)
                .FirstOrDefault(),
            _db.Bookings.Count(b => b.UserId == u.Id && !b.IsDeleted),
            u.CreatedAt);
}
