using MediatR;
using Shuttlez.Application.Locations.DTOs;

namespace Shuttlez.Application.Locations.Queries;

public record GetSavedLocationsQuery : IRequest<IReadOnlyList<SavedLocationDto>>;

public record CreateSavedLocationCommand(CreateSavedLocationRequest Request)
    : IRequest<SavedLocationDto>;

public record UpdateSavedLocationCommand(Guid Id, UpdateSavedLocationRequest Request)
    : IRequest<SavedLocationDto>;

public record DeleteSavedLocationCommand(Guid Id) : IRequest<Unit>;

public record ToggleFavoriteLocationCommand(Guid Id) : IRequest<SavedLocationDto>;
