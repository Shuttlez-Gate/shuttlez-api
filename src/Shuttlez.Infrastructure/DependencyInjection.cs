using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.RouteMatching;
using Shuttlez.Infrastructure.Configuration;
using Shuttlez.Infrastructure.Data;
using Shuttlez.Infrastructure.Identity;
using Shuttlez.Infrastructure.Services;

namespace Shuttlez.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<OtpSettings>(configuration.GetSection(OtpSettings.SectionName));
        services.Configure<RouteMatchingOptions>(configuration.GetSection(RouteMatchingOptions.SectionName));
        services.Configure<GoogleMapsSettings>(configuration.GetSection(GoogleMapsSettings.SectionName));

        services.AddSingleton<IRouteMatchingService, RouteMatchingService>();
        services.AddHttpClient<IGoogleDirectionsService, GoogleDirectionsService>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<IDriverRealtimeNotifier, NullDriverRealtimeNotifier>();
        services.AddSingleton<ISupportChatRealtimeNotifier, NullSupportChatRealtimeNotifier>();
        services.AddMemoryCache();

        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt settings are missing");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        // Invalid/expired tokens must not block public endpoints.
                        context.NoResult();
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = null;
        });

        return services;
    }
}
