using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Common.Interfaces;

public interface IOtpService
{
    Task SendOtpAsync(string phone, OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task<bool> VerifyOtpAsync(string phone, string code, OtpPurpose purpose, CancellationToken cancellationToken = default);
}
