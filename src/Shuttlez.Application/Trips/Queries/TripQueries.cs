using MediatR;
using Shuttlez.Application.Trips.DTOs;

namespace Shuttlez.Application.Trips.Queries;

public record GetUserTripsQuery : IRequest<TripListResponse>;

public record GetTripDetailsQuery(Guid TripId) : IRequest<TripDetailsDto>;

public record GetTripInvoiceQuery(Guid TripId) : IRequest<TripInvoiceDto>;
