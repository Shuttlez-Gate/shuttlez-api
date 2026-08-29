using Shuttlez.Application.Common;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Auth;

public static class SocialAuthProvider
{
    public const string Google = "google";
    public const string Facebook = "facebook";

    public static string Normalize(string provider) => provider.Trim().ToLowerInvariant() switch
    {
        Google => Google,
        Facebook => Facebook,
        _ => throw new AppException(
            "مزود تسجيل الدخول غير مدعوم",
            400,
            "SOCIAL_PROVIDER_UNSUPPORTED")
    };

    public static string? GetLinkedProviderId(User user, string provider) => Normalize(provider) switch
    {
        Google => user.GoogleProviderId,
        Facebook => user.FacebookProviderId,
        _ => null
    };

    public static void SetLinkedProviderId(User user, string provider, string providerUserId)
    {
        switch (Normalize(provider))
        {
            case Google:
                user.GoogleProviderId = providerUserId;
                break;
            case Facebook:
                user.FacebookProviderId = providerUserId;
                break;
        }
    }
}
