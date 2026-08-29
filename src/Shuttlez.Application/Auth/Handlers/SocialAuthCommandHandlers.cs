using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Auth.Commands;
using Shuttlez.Application.Auth.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Auth.Handlers;

internal static class SocialAuthHelpers
{
    public static async Task<User?> FindLinkedUserAsync(
        IAppDbContext db,
        string provider,
        string providerUserId,
        CancellationToken ct)
    {
        provider = SocialAuthProvider.Normalize(provider);
        return provider switch
        {
            SocialAuthProvider.Google => await db.Users
                .FirstOrDefaultAsync(
                    u => u.GoogleProviderId == providerUserId && !u.IsDeleted,
                    ct),
            SocialAuthProvider.Facebook => await db.Users
                .FirstOrDefaultAsync(
                    u => u.FacebookProviderId == providerUserId && !u.IsDeleted,
                    ct),
            _ => null
        };
    }

    public static void EnsureUserIsActive(User user)
    {
        if (!user.IsActive)
        {
            throw new ForbiddenAppException(
                "هذا الحساب غير نشط",
                "ACCOUNT_DISABLED");
        }
    }

    public static void EnsureProviderNotLinkedToAnotherUser(
        User? linkedUser,
        User? targetUser)
    {
        if (linkedUser is null)
        {
            return;
        }

        if (targetUser is null || linkedUser.Id != targetUser.Id)
        {
            throw new AppException(
                "هذا الحساب الاجتماعي مرتبط بالفعل بحساب آخر",
                409,
                "FIREBASE_UID_ALREADY_LINKED");
        }
    }

    public static void MergeSocialProfile(User user, VerifiedSocialIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(identity.DisplayName))
        {
            user.FullName = identity.DisplayName!.Trim();
        }

        if (string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(identity.Email))
        {
            user.Email = identity.Email!.Trim();
        }

        if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(identity.PhotoUrl))
        {
            user.AvatarUrl = identity.PhotoUrl!.Trim();
        }
    }

    public static async Task<AuthTokensDto> IssueTokensAsync(
        IAppDbContext db,
        IJwtTokenService jwt,
        IDateTimeProvider clock,
        User user,
        string? ip,
        CancellationToken ct)
    {
        var accessToken = jwt.GenerateAccessToken(user);
        var refreshTokenValue = jwt.GenerateRefreshToken();
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = clock.UtcNow.AddDays(30),
            CreatedByIp = ip
        };

        db.Add(refresh);
        await db.SaveChangesAsync(ct);

        return new AuthTokensDto(
            accessToken,
            refreshTokenValue,
            clock.UtcNow.AddHours(1),
            refresh.ExpiresAt);
    }
}

public class SocialLoginCommandHandler : IRequestHandler<SocialLoginCommand, SocialLoginResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IFirebaseTokenVerifier _firebase;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;

    public SocialLoginCommandHandler(
        IAppDbContext db,
        IFirebaseTokenVerifier firebase,
        IJwtTokenService jwt,
        IDateTimeProvider clock)
    {
        _db = db;
        _firebase = firebase;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<SocialLoginResultDto> Handle(
        SocialLoginCommand request,
        CancellationToken cancellationToken)
    {
        var identity = await _firebase.VerifyAsync(
            request.Request.Provider,
            request.Request.FirebaseIdToken,
            cancellationToken);

        var linkedUser = await SocialAuthHelpers.FindLinkedUserAsync(
            _db,
            identity.Provider,
            identity.ProviderUserId,
            cancellationToken);

        if (linkedUser is null)
        {
            return new SocialLoginResultDto(
                true,
                identity.Provider,
                null,
                identity.DisplayName,
                identity.Email,
                identity.PhotoUrl);
        }

        SocialAuthHelpers.EnsureUserIsActive(linkedUser);
        var tokens = await SocialAuthHelpers.IssueTokensAsync(
            _db,
            _jwt,
            _clock,
            linkedUser,
            request.IpAddress,
            cancellationToken);

        return new SocialLoginResultDto(
            false,
            identity.Provider,
            new AuthResponseDto(tokens, AuthMapper.ToProfileDto(linkedUser)));
    }
}

public class SocialSendOtpCommandHandler : IRequestHandler<SocialSendOtpCommand, SendOtpResponseDto>
{
    private readonly IFirebaseTokenVerifier _firebase;
    private readonly IOtpService _otpService;
    private readonly IAppDbContext _db;

    public SocialSendOtpCommandHandler(
        IFirebaseTokenVerifier firebase,
        IOtpService otpService,
        IAppDbContext db)
    {
        _firebase = firebase;
        _otpService = otpService;
        _db = db;
    }

    public async Task<SendOtpResponseDto> Handle(
        SocialSendOtpCommand request,
        CancellationToken cancellationToken)
    {
        var identity = await _firebase.VerifyAsync(
            request.Request.Provider,
            request.Request.FirebaseIdToken,
            cancellationToken);

        var linkedUser = await SocialAuthHelpers.FindLinkedUserAsync(
            _db,
            identity.Provider,
            identity.ProviderUserId,
            cancellationToken);

        if (linkedUser is not null)
        {
            SocialAuthHelpers.EnsureUserIsActive(linkedUser);
            throw new AppException(
                "هذا الحساب الاجتماعي مرتبط بالفعل ويمكنك تسجيل الدخول مباشرة",
                409,
                "ACCOUNT_ALREADY_LINKED");
        }

        var result = await _otpService.SendOtpAsync(
            request.Request.Phone,
            OtpPurpose.SocialLink,
            cancellationToken);

        return new SendOtpResponseDto(result.Message, result.DebugCode);
    }
}

public class SocialCompleteCommandHandler : IRequestHandler<SocialCompleteCommand, AuthResponseDto>
{
    private readonly IAppDbContext _db;
    private readonly IFirebaseTokenVerifier _firebase;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;

    public SocialCompleteCommandHandler(
        IAppDbContext db,
        IFirebaseTokenVerifier firebase,
        IOtpService otpService,
        IJwtTokenService jwt,
        IDateTimeProvider clock)
    {
        _db = db;
        _firebase = firebase;
        _otpService = otpService;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<AuthResponseDto> Handle(
        SocialCompleteCommand request,
        CancellationToken cancellationToken)
    {
        var identity = await _firebase.VerifyAsync(
            request.Request.Provider,
            request.Request.FirebaseIdToken,
            cancellationToken);

        var valid = await _otpService.VerifyOtpAsync(
            request.Request.Phone,
            request.Request.Code,
            OtpPurpose.SocialLink,
            cancellationToken);

        if (!valid)
        {
            throw new UnauthorizedAppException(
                "رمز التحقق غير صحيح أو منتهي الصلاحية",
                "OTP_INVALID");
        }

        var phone = PhoneNormalizer.Normalize(request.Request.Phone);

        var linkedUser = await SocialAuthHelpers.FindLinkedUserAsync(
            _db,
            identity.Provider,
            identity.ProviderUserId,
            cancellationToken);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted, cancellationToken);

        SocialAuthHelpers.EnsureProviderNotLinkedToAnotherUser(linkedUser, user);

        if (user is null)
        {
            user = new User
            {
                Phone = phone,
                FullName = string.IsNullOrWhiteSpace(identity.DisplayName)
                    ? phone
                    : identity.DisplayName!.Trim(),
                Email = string.IsNullOrWhiteSpace(identity.Email)
                    ? null
                    : identity.Email!.Trim(),
                AvatarUrl = string.IsNullOrWhiteSpace(identity.PhotoUrl)
                    ? null
                    : identity.PhotoUrl!.Trim(),
                UserType = UserType.Passenger
            };

            SocialAuthProvider.SetLinkedProviderId(user, identity.Provider, identity.ProviderUserId);
            _db.Add(user);
            _db.Add(new Wallet { UserId = user.Id, Balance = 0 });
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            SocialAuthHelpers.EnsureUserIsActive(user);
            SocialAuthHelpers.MergeSocialProfile(user, identity);
            SocialAuthProvider.SetLinkedProviderId(user, identity.Provider, identity.ProviderUserId);
            _db.Update(user);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var tokens = await SocialAuthHelpers.IssueTokensAsync(
            _db,
            _jwt,
            _clock,
            user,
            request.IpAddress,
            cancellationToken);

        return new AuthResponseDto(tokens, AuthMapper.ToProfileDto(user));
    }
}
