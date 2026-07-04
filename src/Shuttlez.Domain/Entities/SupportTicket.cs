using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class SupportTicket : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? TripId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public DateTime? ClosedAt { get; set; }

    public User User { get; set; } = null!;
    public Trip? Trip { get; set; }
    public ICollection<SupportMessage> Messages { get; set; } = [];
}
