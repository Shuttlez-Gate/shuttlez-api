using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Auth.Commands;
using Shuttlez.Application.Auth.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Auth.Handlers;

public class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, string>
{
    private readonly IOtpService _otpService;
    private readonly IAppDbContext _db;

    public SendOtpCommandHandler(IOtpService otpService, IAppDbContext db)
    {
        _otpService = otpService;
        _db = db;
    }

    public async Task<string> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        var purpose = AuthMapper.ParsePurpose(request.Request.Purpose);
        var phone = PhoneNormalizer.Normalize(request.Request.Phone);

        if (purpose == OtpPurpose.Login)
        {
            var exists = await _db.Users
                .AnyAsync(u => u.Phone == phone && !u.IsDeleted, cancellationToken);

            if (!exists)
            {
                throw new AppException("رقم الهاتف غير مسجل. أنشئ حساباً أولاً");
            }
        }

        if (purpose == OtpPurpose.Register)
        {
            var exists = await _db.Users
                .AnyAsync(u => u.Phone == phone && !u.IsDeleted, cancellationToken);

            if (exists)
            {
                throw new AppException("رقم الهاتف مسجل بالفعل. سجّل الدخول بدلاً من ذلك");
            }
        }

        await _otpService.SendOtpAsync(phone, purpose, cancellationToken);
        return "تم إرسال رمز التحقق";
    }
}

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, AuthResponseDto>
{
    private readonly IAppDbContext _db;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;

    public VerifyOtpCommandHandler(
        IAppDbContext db,
        IOtpService otpService,
        IJwtTokenService jwt,
        IDateTimeProvider clock)
    {
        _db = db;
        _otpService = otpService;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<AuthResponseDto> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var purpose = AuthMapper.ParsePurpose(request.Request.Purpose);
        var phone = PhoneNormalizer.Normalize(request.Request.Phone);

        var valid = await _otpService.VerifyOtpAsync(
            phone,
            request.Request.Code,
            purpose,
            cancellationToken);

        if (!valid)
        {
            throw new UnauthorizedAppException("رمز التحقق غير صحيح أو منتهي الصلاحية");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            throw new AppException("رقم الهاتف غير مسجل. أنشئ حساباً أولاً");
        }

        var tokens = await IssueTokensAsync(user, request.IpAddress, cancellationToken);
        return new AuthResponseDto(tokens, AuthMapper.ToProfileDto(user));
    }

    private async Task<AuthTokensDto> IssueTokensAsync(User user, string? ip, CancellationToken ct)
    {
        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshTokenValue = _jwt.GenerateRefreshToken();
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _clock.UtcNow.AddDays(30),
            CreatedByIp = ip
        };
        _db.Add(refresh);
        await _db.SaveChangesAsync(ct);

        return new AuthTokensDto(
            accessToken,
            refreshTokenValue,
            _clock.UtcNow.AddHours(1),
            refresh.ExpiresAt);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IAppDbContext _db;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;

    public RegisterCommandHandler(
        IAppDbContext db,
        IOtpService otpService,
        IJwtTokenService jwt,
        IDateTimeProvider clock)
    {
        _db = db;
        _otpService = otpService;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var phone = PhoneNormalizer.Normalize(request.Request.Phone);

        var valid = await _otpService.VerifyOtpAsync(
            phone,
            request.Request.Code,
            OtpPurpose.Register,
            cancellationToken);

        if (!valid)
        {
            throw new UnauthorizedAppException("رمز التحقق غير صحيح أو منتهي الصلاحية");
        }

        var existing = await _db.Users
            .FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted, cancellationToken);

        if (existing is not null)
        {
            throw new AppException("رقم الهاتف مسجل بالفعل");
        }

        var user = new User
        {
            Phone = phone,
            FullName = request.Request.FullName,
            Email = request.Request.Email,
            Gender = AuthMapper.ParseGender(request.Request.Gender),
            UserType = UserType.Passenger
        };

        _db.Add(user);
        _db.Add(new Wallet { UserId = user.Id, Balance = 0 });
        await _db.SaveChangesAsync(cancellationToken);

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshTokenValue = _jwt.GenerateRefreshToken();
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _clock.UtcNow.AddDays(30),
            CreatedByIp = request.IpAddress
        };
        _db.Add(refresh);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            new AuthTokensDto(accessToken, refreshTokenValue, _clock.UtcNow.AddHours(1), refresh.ExpiresAt),
            AuthMapper.ToProfileDto(user));
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;

    public RefreshTokenCommandHandler(IAppDbContext db, IJwtTokenService jwt, IDateTimeProvider clock)
    {
        _db = db;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<AuthTokensDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.Request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedAppException("Refresh token غير صالح");

        if (!token.IsActive)
        {
            throw new UnauthorizedAppException("Refresh token منتهي أو ملغي");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == token.UserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("المستخدم غير موجود");

        token.RevokedAt = _clock.UtcNow;
        token.ReplacedByToken = _jwt.GenerateRefreshToken();

        var newRefresh = new RefreshToken
        {
            UserId = user.Id,
            Token = token.ReplacedByToken,
            ExpiresAt = _clock.UtcNow.AddDays(30),
            CreatedByIp = request.IpAddress
        };

        _db.Add(newRefresh);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(
            _jwt.GenerateAccessToken(user),
            newRefresh.Token,
            _clock.UtcNow.AddHours(1),
            newRefresh.ExpiresAt);
    }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public LogoutCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (token is not null && token.IsActive)
        {
            token.RevokedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
