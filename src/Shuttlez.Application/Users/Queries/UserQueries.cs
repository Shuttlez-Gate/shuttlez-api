using MediatR;
using Shuttlez.Application.Auth.DTOs;

namespace Shuttlez.Application.Users.Queries;

public record GetCurrentUserQuery : IRequest<UserProfileDto>;

public record UpdateProfileCommand(
    string? FullName,
    string? Email,
    string? Gender,
    string? AvatarUrl) : IRequest<UserProfileDto>;
