using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid UserId { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public int SeatCount { get; set; } = 1;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public string? ReferenceCode { get; set; }

    /// Snapshot of trip seat price at booking time.
    public decimal PricePerSeat { get; set; }

    /// Platform commission rate applied (0–1).
    public decimal CommissionRate { get; set; }

    public decimal CommissionAmount { get; set; }
    public decimal CaptainEarnings { get; set; }

    /// When true, this booking consumes subscription trip credits.
    public bool UsesSubscriptionCredit { get; set; }

    public Trip Trip { get; set; } = null!;
    public User User { get; set; } = null!;
    public Invoice? Invoice { get; set; }
}
