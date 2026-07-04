using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Locations.DTOs;
using Shuttlez.Application.Locations.Queries;
using Shuttlez.Domain.Entities;

namespace Shuttlez.Application.Locations.Handlers;

public class LocationHandlers :
    IRequestHandler<GetSavedLocationsQuery, IReadOnlyList<SavedLocationDto>>,
    IRequestHandler<CreateSavedLocationCommand, SavedLocationDto>,
    IRequestHandler<UpdateSavedLocationCommand, SavedLocationDto>,
    IRequestHandler<DeleteSavedLocationCommand, Unit>,
    IRequestHandler<ToggleFavoriteLocationCommand, SavedLocationDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public LocationHandlers(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<SavedLocationDto>> Handle(
        GetSavedLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var items = await _db.SavedLocations
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.IsFavorite)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<SavedLocationDto> Handle(
        CreateSavedLocationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var entity = new SavedLocation
        {
            UserId = userId,
            Label = request.Request.Label.Trim(),
            Address = request.Request.Address.Trim(),
            Latitude = request.Request.Latitude,
            Longitude = request.Request.Longitude,
            IsFavorite = request.Request.IsFavorite
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<SavedLocationDto> Handle(
        UpdateSavedLocationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var entity = await FindOwnedLocation(userId, request.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Request.Label))
            entity.Label = request.Request.Label.Trim();
        if (!string.IsNullOrWhiteSpace(request.Request.Address))
            entity.Address = request.Request.Address.Trim();
        if (request.Request.Latitude.HasValue)
            entity.Latitude = request.Request.Latitude.Value;
        if (request.Request.Longitude.HasValue)
            entity.Longitude = request.Request.Longitude.Value;
        if (request.Request.IsFavorite.HasValue)
            entity.IsFavorite = request.Request.IsFavorite.Value;

        entity.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<Unit> Handle(
        DeleteSavedLocationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var entity = await FindOwnedLocation(userId, request.Id, cancellationToken);
        entity.IsDeleted = true;
        entity.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<SavedLocationDto> Handle(
        ToggleFavoriteLocationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var entity = await FindOwnedLocation(userId, request.Id, cancellationToken);
        entity.IsFavorite = !entity.IsFavorite;
        entity.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAppException("غير مصرح");

    private async Task<SavedLocation> FindOwnedLocation(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await _db.SavedLocations
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted, cancellationToken);

        return entity ?? throw new NotFoundException("الموقع غير موجود");
    }

    private static SavedLocationDto Map(SavedLocation entity) => new(
        entity.Id,
        entity.Label,
        entity.Address,
        entity.Latitude,
        entity.Longitude,
        entity.IsFavorite,
        entity.CreatedAt);
}
