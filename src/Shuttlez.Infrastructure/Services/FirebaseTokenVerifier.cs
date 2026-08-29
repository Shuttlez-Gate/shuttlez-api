using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using Shuttlez.Application.Auth;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Infrastructure.Configuration;

namespace Shuttlez.Infrastructure.Services;

public class FirebaseTokenVerifier : IFirebaseTokenVerifier
{
    private readonly FirebaseAuthSettings _settings;
    private readonly Lazy<FirebaseApp?> _app;

    public FirebaseTokenVerifier(IOptions<FirebaseAuthSettings> settings)
    {
        _settings = settings.Value;
        _app = new Lazy<FirebaseApp?>(CreateApp);
    }

    public async Task<VerifiedSocialIdentity> VerifyAsync(
        string provider,
        string firebaseIdToken,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = SocialAuthProvider.Normalize(provider);
        var app = _app.Value ?? throw new AppException(
            "خدمة تسجيل الدخول الاجتماعي غير مهيأة على الخادم",
            503,
            "SOCIAL_AUTH_NOT_CONFIGURED");

        FirebaseToken decoded;
        try
        {
            decoded = await FirebaseAuth.GetAuth(app)
                .VerifyIdTokenAsync(firebaseIdToken, cancellationToken);
        }
        catch
        {
            throw new UnauthorizedAppException(
                "رمز Firebase غير صالح أو منتهي الصلاحية",
                "FIREBASE_TOKEN_INVALID");
        }

        var claims = decoded.Claims;
        var signInProvider = claims.TryGetValue("firebase", out var firebaseClaim)
            && firebaseClaim is IReadOnlyDictionary<string, object> firebaseData
            && firebaseData.TryGetValue("sign_in_provider", out var providerValue)
                ? providerValue?.ToString()
                : null;

        if (!string.Equals(signInProvider, normalizedProvider, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAppException(
                "نوع مزود تسجيل الدخول لا يطابق الرمز المرسل",
                "FIREBASE_PROVIDER_MISMATCH");
        }

        var displayName = claims.TryGetValue("name", out var name) ? name?.ToString() : null;
        var email = claims.TryGetValue("email", out var emailValue) ? emailValue?.ToString() : null;
        var photoUrl = claims.TryGetValue("picture", out var picture) ? picture?.ToString() : null;

        return new VerifiedSocialIdentity(
            normalizedProvider,
            decoded.Uid,
            displayName,
            email,
            photoUrl);
    }

    private FirebaseApp? CreateApp()
    {
        try
        {
            if (FirebaseApp.DefaultInstance is not null)
            {
                return FirebaseApp.DefaultInstance;
            }
        }
        catch
        {
        }

        var credentialsPath = _settings.CredentialsPath;
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        }

        if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
        {
            return null;
        }

        var options = new AppOptions
        {
            Credential = GoogleCredential.FromFile(credentialsPath)
        };

        if (!string.IsNullOrWhiteSpace(_settings.ProjectId))
        {
            options.ProjectId = _settings.ProjectId;
        }

        return FirebaseApp.Create(options, "ShuttlezSocialAuth");
    }
}
