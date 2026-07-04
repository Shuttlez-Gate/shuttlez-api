using System.Net;
using System.Text.Json;
using FluentValidation;
using Shuttlez.Application.Common;

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
        var (statusCode, message, errors) = exception switch
        {
            AppException appEx => (appEx.StatusCode, appEx.Message, new[] { appEx.Message }),
            ValidationException validationEx => (
                400,
                "بيانات غير صالحة",
                validationEx.Errors.Select(e => e.ErrorMessage).ToArray()),
            _ => (500, "حدث خطأ غير متوقع", new[] { "Internal Server Error" })
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
            Errors = errors
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
