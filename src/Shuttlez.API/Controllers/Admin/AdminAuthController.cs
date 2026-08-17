using MediatR;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.API.Filters;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Auth.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Infrastructure.Configuration;

namespace Shuttlez.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/auth")]
[EnableCors(CorsSettings.PolicyName)]
public class AdminAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>يرسل رمز تحقق لرقم مسؤول مسجّل فقط.</summary>
    [HttpPost("send-otp")]
    public async Task<ActionResult<ApiResponse<SendOtpResponseDto>>> SendOtp(
        [FromBody] AdminSendOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SendAdminOtpCommand(request.Phone), cancellationToken);
        var dto = new SendOtpResponseDto(result.Message, result.DebugCode);
        return Ok(ApiResponse<SendOtpResponseDto>.Ok(dto, result.Message));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AdminLoginResponse>>> Login(
        [FromBody] AdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new AdminLoginCommand(request, ip), cancellationToken);
        return Ok(ApiResponse<AdminLoginResponse>.Ok(result, "تم تسجيل الدخول"));
    }

    [HttpGet("me")]
    [AdminOnly]
    public async Task<ActionResult<ApiResponse<AdminIdentityDto>>> Me(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AdminMeQuery(), cancellationToken);
        return Ok(ApiResponse<AdminIdentityDto>.Ok(result));
    }
}

public record AdminSendOtpRequest(string Phone);
