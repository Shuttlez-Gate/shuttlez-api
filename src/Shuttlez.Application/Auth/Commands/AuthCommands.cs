using MediatR;
using Shuttlez.Application.Auth.DTOs;

namespace Shuttlez.Application.Auth.Commands;

public record SendOtpCommand(SendOtpRequest Request) : IRequest<string>;

public record VerifyOtpCommand(VerifyOtpRequest Request, string? IpAddress) : IRequest<AuthResponseDto>;

public record RegisterCommand(RegisterRequest Request, string? IpAddress) : IRequest<AuthResponseDto>;

public record RefreshTokenCommand(RefreshTokenRequest Request, string? IpAddress) : IRequest<AuthTokensDto>;

public record LogoutCommand(string RefreshToken) : IRequest<Unit>;
