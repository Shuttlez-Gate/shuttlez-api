using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Shuttlez.Application.Common;
using Shuttlez.Infrastructure.Configuration;

namespace Shuttlez.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        await ApplyCorsHeadersAsync(context);

        var (statusCode, message, errors, code) = exception switch
        {
            AppException appEx => (appEx.StatusCode, appEx.Message, new[] { appEx.Message }, appEx.Code),
            ValidationException validationEx => (
                400,
                "بيانات غير صالحة",
                validationEx.Errors.Select(e => e.ErrorMessage).ToArray(),
                "VALIDATION_ERROR"),
            _ => (500, "حدث خطأ غير متوقع", new[] { SafeError(exception) }, "INTERNAL_SERVER_ERROR")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ApiResponse
        {
            Success = false,
            Message = message,
            Code = code,
            Errors = errors
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static async Task ApplyCorsHeadersAsync(HttpContext context)
    {
        var corsPolicyProvider = context.RequestServices.GetService<ICorsPolicyProvider>();
        var corsService = context.RequestServices.GetService<ICorsService>();

        if (corsPolicyProvider is null || corsService is null)
        {
            return;
        }

        var policy = await corsPolicyProvider.GetPolicyAsync(context, CorsSettings.PolicyName);
        if (policy is null)
        {
            return;
        }

        var result = corsService.EvaluatePolicy(context, policy);
        corsService.ApplyResult(result, context.Response);
    }

    private static string SafeError(Exception exception)
    {
        var parts = new List<string> { $"{exception.GetType().Name}: {exception.Message}" };
        if (exception.InnerException is { } inner)
        {
            parts.Add($"{inner.GetType().Name}: {inner.Message}");
        }

        var text = string.Join(" | ", parts);
        return System.Text.RegularExpressions.Regex.Replace(
            text,
            @"(Password|Pwd)\s*=\s*[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
