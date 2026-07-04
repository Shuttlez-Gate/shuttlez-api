using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Auth;
using Shuttlez.Application.Auth.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Users.Queries;

namespace Shuttlez.Application.Users.Handlers;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<UserProfileDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("المستخدم غير موجود");

        return AuthMapper.ToProfileDto(user);
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public UpdateProfileCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("المستخدم غير موجود");

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        if (request.Email is not null)
        {
            user.Email = request.Email.Trim();
        }

        if (request.Gender is not null)
        {
            user.Gender = AuthMapper.ParseGender(request.Gender);
        }

        if (request.AvatarUrl is not null)
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        user.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return AuthMapper.ToProfileDto(user);
    }
}
