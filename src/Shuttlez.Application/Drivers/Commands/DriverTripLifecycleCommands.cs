using MediatR;
using Shuttlez.Application.Drivers.DTOs;

namespace Shuttlez.Application.Drivers.Commands;

public record StartMyDriverTripCommand(Guid TripId) : IRequest<DriverTripLifecycleDto>;

public record CompleteMyDriverTripCommand(Guid TripId) : IRequest<DriverTripLifecycleDto>;
