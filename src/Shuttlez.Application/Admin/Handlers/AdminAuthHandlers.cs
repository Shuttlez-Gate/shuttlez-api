using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin.Handlers;

public record SendAdminOtpCommand(string Phone) : IRequest<SendOtpResult>;

public record AdminLoginCommand(AdminLoginRequest Request, string? IpAddress)
    : IRequest<AdminLoginResponse>;

public record AdminMeQuery : IRequest<AdminIdentityDto>;

public class AdminAuthHandlers :
    IRequestHandler<SendAdminOtpCommand, SendOtpResult>,
    IRequestHandler<AdminLoginCommand, AdminLoginResponse>,
    IRequestHandler<AdminMeQuery, AdminIdentityDto>
{
    private readonly IAppDbContext _db;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;

    public AdminAuthHandlers(
        IAppDbContext db,
        IOtpService otpService,
        IJwtTokenService jwt,
        IDateTimeProvider clock,
        ICurrentUserService currentUser)
    {
        _db = db;
        _otpService = otpService;
        _jwt = jwt;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<SendOtpResult> Handle(
        SendAdminOtpCommand request,
        CancellationToken cancellationToken)
    {
        var phone = PhoneNormalizer.Normalize(request.Phone);
        var isAdmin = await _db.Users.AnyAsync(
            u => u.Phone == phone
                && !u.IsDeleted
                && u.IsActive
                && u.UserType == UserType.Admin,
            cancellationToken);

        if (!isAdmin)
        {
            throw new UnauthorizedAppException("هذا الرقم غير مصرّح له بالدخول للوحة التحكم");
        }

        return await _otpService.SendOtpAsync(phone, OtpPurpose.Login, cancellationToken);
    }

    public async Task<AdminLoginResponse> Handle(
        AdminLoginCommand request,
        CancellationToken cancellationToken)
    {
        var phone = PhoneNormalizer.Normalize(request.Request.Phone);

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Phone == phone && !u.IsDeleted,
            cancellationToken)
            ?? throw new UnauthorizedAppException("بيانات الدخول غير صحيحة");

        if (user.UserType != UserType.Admin)
        {
            throw new ForbiddenAppException("هذا الحساب ليس حساب مسؤول");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenAppException("الحساب موقوف. راجع مسؤول النظام");
        }

        var valid = await _otpService.VerifyOtpAsync(
            phone,
            request.Request.Code,
            OtpPurpose.Login,
            cancellationToken);

        if (!valid)
        {
            throw new UnauthorizedAppException("رمز التحقق غير صحيح أو منتهي");
        }

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshValue = _jwt.GenerateRefreshToken();
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshValue,
            ExpiresAt = _clock.UtcNow.AddDays(30),
            CreatedByIp = request.IpAddress
        };
        _db.Add(refresh);
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminLoginResponse(
            accessToken,
            refreshValue,
            _clock.UtcNow.AddHours(1),
            refresh.ExpiresAt,
            ToIdentity(user));
    }

    public async Task<AdminIdentityDto> Handle(
        AdminMeQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("يجب تسجيل الدخول");

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == userId && !u.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("الحساب غير موجود");

        if (user.UserType != UserType.Admin)
        {
            throw new ForbiddenAppException("هذا الحساب ليس حساب مسؤول");
        }

        return ToIdentity(user);
    }

    private static AdminIdentityDto ToIdentity(User user) => new(
        user.Id,
        user.Phone,
        user.FullName,
        user.Email,
        user.AvatarUrl,
        user.UserType.ToString());
}
