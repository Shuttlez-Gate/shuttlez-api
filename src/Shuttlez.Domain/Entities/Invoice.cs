using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }

    public Booking Booking { get; set; } = null!;
}
