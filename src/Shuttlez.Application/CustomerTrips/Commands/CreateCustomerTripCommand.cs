using MediatR;
using Shuttlez.Application.Common;
using Shuttlez.Application.CustomerTrips.DTOs;

namespace Shuttlez.Application.CustomerTrips.Commands;

public record CreateCustomerTripCommand(CreateCustomerTripRequest Request)
    : IRequest<CreateCustomerTripResponse>;

/// <summary>
/// Phase 6A soft-deprecation. Former handler matched routes or auto-created
/// Route+Trip with hardcoded PricePerSeat=120 / AvailableSeats=14 outside Admin
/// READY launch — not a valid Ride product. Do not re-enable without a real
/// Ride pricing + CASH lifecycle contract.
/// </summary>
public class CreateCustomerTripHandler : IRequestHandler<CreateCustomerTripCommand, CreateCustomerTripResponse>
{
    public Task<CreateCustomerTripResponse> Handle(
        CreateCustomerTripCommand request,
        CancellationToken cancellationToken)
    {
        _ = request;
        _ = cancellationToken;
        throw new AppException(
            "هذا المسار متوقف. حجز الشاتل عبر /api/v1/bookings فقط. منتج Ride يحتاج عقد تسعير وتشغيل منفصل.",
            410,
            ErrorCodes.CustomerTripsDeprecated);
    }
}
