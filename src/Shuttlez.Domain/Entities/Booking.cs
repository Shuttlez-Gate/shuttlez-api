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

    public Trip Trip { get; set; } = null!;
    public User User { get; set; } = null!;
    public Invoice? Invoice { get; set; }
}
