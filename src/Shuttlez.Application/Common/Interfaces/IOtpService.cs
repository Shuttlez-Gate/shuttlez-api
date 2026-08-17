using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Common.Interfaces;

public record SendOtpResult(string Message, string? DebugCode = null);

public interface IOtpService
{
    Task<SendOtpResult> SendOtpAsync(string phone, OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task<bool> VerifyOtpAsync(string phone, string code, OtpPurpose purpose, CancellationToken cancellationToken = default);
}
