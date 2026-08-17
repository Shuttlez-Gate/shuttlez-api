using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shuttlez.API.Hubs;
using Shuttlez.API.Middleware;
using Shuttlez.API.Services;
using Shuttlez.Application;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Infrastructure;
using Shuttlez.Infrastructure.Configuration;
using Shuttlez.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AllowAnonymousFilter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Shuttlez API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var corsSettings = builder.Configuration
    .GetSection(CorsSettings.SectionName)
    .Get<CorsSettings>() ?? new CorsSettings();

var allowedOrigins = corsSettings.AllowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must include at least one origin (e.g. https://shuttlez.org).");
}

builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));
builder.Services.Configure<Shuttlez.Application.Landing.CaptainLaunchOfferSettings>(
    builder.Configuration.GetSection(Shuttlez.Application.Landing.CaptainLaunchOfferSettings.SectionName));
builder.Services.AddSignalR();
builder.Services.AddSingleton<IDriverRealtimeNotifier, DriverRealtimeNotifier>();
builder.Services.AddSingleton<ISupportChatRealtimeNotifier, SupportChatRealtimeNotifier>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsSettings.PolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Database migrate/seed failed on startup — API will still start.");
        try
        {
            var logDir = Path.Combine(app.Environment.ContentRootPath, "logs");
            Directory.CreateDirectory(logDir);
            await File.WriteAllTextAsync(
                Path.Combine(logDir, "startup-error.txt"),
                $"{DateTime.UtcNow:O}{Environment.NewLine}{ex}{Environment.NewLine}");
        }
        catch
        {
            // Best-effort file log for shared hosting when stdout is unavailable.
        }
    }
}

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors(CorsSettings.PolicyName);

var webRoot = app.Environment.WebRootPath;
if (string.IsNullOrWhiteSpace(webRoot))
{
    webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(webRoot);
}

Directory.CreateDirectory(Path.Combine(webRoot, "uploads"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot),
    RequestPath = ""
});

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
            | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
    });
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<DriverTripsRateLimitMiddleware>();

app.MapControllers();
app.MapHub<TripTrackingHub>("/hubs/trip-tracking");
app.MapHub<SupportChatHub>("/hubs/support-chat");
app.MapHub<DriverHub>("/hubs/driver");

app.Run();

public partial class Program;
