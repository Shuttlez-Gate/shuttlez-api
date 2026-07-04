namespace Shuttlez.Application.Bookings.DTOs;

public record PreviewDateChipDto(
    string DayName,
    string ShortDate,
    bool IsSelected);

public record ShuttleOfferDto(
    Guid Id,
    string PickupWalkLabel,
    string PickupAddress,
    string PickupTime,
    string DropoffAddress,
    string DropoffTime,
    string DropoffWalkLabel,
    string PlateLabel,
    string BadgeVariant,
    string CrossedPrice,
    string PackageLabel,
    int PackageLabelArgb,
    string SeatsLabel,
    int SeatsArgb,
    bool SeatsStrikethrough,
    bool CardDimmed);

public record BookingPreviewDto(
    string SourceAddress,
    string DestinationAddress,
    IReadOnlyList<PreviewDateChipDto> DateChips,
    IReadOnlyList<IReadOnlyList<ShuttleOfferDto>> OffersPerDay);

public record CreateBookingRequest(
    Guid TripId,
    int SeatCount = 1,
    string PaymentMethod = "cash");

public record CreateBookingResponse(
    Guid BookingId,
    Guid TripId,
    string ReferenceCode,
    decimal TotalAmount,
    string Status,
    string Message);
