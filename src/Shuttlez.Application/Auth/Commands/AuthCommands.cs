using MediatR;
using Shuttlez.Application.Auth.DTOs;

namespace Shuttlez.Application.Auth.Commands;

public record SendOtpCommand(SendOtpRequest Request) : IRequest<SendOtpResponseDto>;

public record VerifyOtpCommand(VerifyOtpRequest Request, string? IpAddress) : IRequest<AuthResponseDto>;

public record RegisterCommand(RegisterRequest Request, string? IpAddress) : IRequest<AuthResponseDto>;

public record RefreshTokenCommand(RefreshTokenRequest Request, string? IpAddress) : IRequest<AuthTokensDto>;

public record LogoutCommand(string RefreshToken) : IRequest<Unit>;

public record SocialLoginCommand(SocialLoginRequest Request, string? IpAddress) : IRequest<SocialLoginResultDto>;

public record SocialSendOtpCommand(SocialSendOtpRequest Request) : IRequest<SendOtpResponseDto>;

public record SocialCompleteCommand(SocialCompleteRequest Request, string? IpAddress) : IRequest<AuthResponseDto>;
