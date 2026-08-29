namespace Shuttlez.Application.Common.Interfaces;

public record VerifiedSocialIdentity(
    string Provider,
    string ProviderUserId,
    string? DisplayName,
    string? Email,
    string? PhotoUrl);

public interface IFirebaseTokenVerifier
{
    Task<VerifiedSocialIdentity> VerifyAsync(
        string provider,
        string firebaseIdToken,
        CancellationToken cancellationToken = default);
}
