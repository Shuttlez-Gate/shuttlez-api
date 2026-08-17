using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Auth.Commands;
using Shuttlez.Application.Auth.DTOs;
using Shuttlez.Application.Common;

namespace Shuttlez.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("send-otp")]
    public async Task<ActionResult<ApiResponse<SendOtpResponseDto>>> SendOtp(
        [FromBody] SendOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SendOtpCommand(request), cancellationToken);
        return Ok(ApiResponse<SendOtpResponseDto>.Ok(result, result.Message));
    }

    [HttpPost("verify-otp")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new VerifyOtpCommand(request, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "تم تسجيل الدخول بنجاح"));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegisterCommand(request, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "تم إنشاء الحساب بنجاح"));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthTokensDto>>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RefreshTokenCommand(request, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return Ok(ApiResponse<AuthTokensDto>.Ok(result));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return Ok(ApiResponse.Ok("تم تسجيل الخروج"));
    }
}
