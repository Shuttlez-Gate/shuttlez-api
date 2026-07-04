using MediatR;
using Shuttlez.Application.Bookings.DTOs;

namespace Shuttlez.Application.Bookings.Queries;

public record GetBookingPreviewQuery(
    double SourceLatitude,
    double SourceLongitude,
    double DestinationLatitude,
    double DestinationLongitude,
    string? SourceAddress,
    string? DestinationAddress,
    string? SourceTime,
    string? DestinationTime,
    int VehicleTypeIndex) : IRequest<BookingPreviewDto>;

public record CreateBookingCommand(CreateBookingRequest Request) : IRequest<CreateBookingResponse>;
