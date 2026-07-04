using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Auth.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Users.Queries;

namespace Shuttlez.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetMe(CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    [HttpPatch("me")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateMe(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(
            new UpdateProfileCommand(request.FullName, request.Email, request.Gender, request.AvatarUrl),
            cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile, "تم تحديث الملف الشخصي"));
    }
}

public record UpdateProfileRequest(
    string? FullName,
    string? Email,
    string? Gender,
    string? AvatarUrl);
