using MediatR;
using Shuttlez.Application.Drivers.DTOs;

namespace Shuttlez.Application.Drivers.Queries;

public record GetMyDriverRatingsQuery : IRequest<DriverRatingsSummaryDto>;

public record GetMyDriverProfileQuery : IRequest<DriverProfileDto>;

public record GetMyDriverTripsQuery : IRequest<DriverTripListResponseDto>;
