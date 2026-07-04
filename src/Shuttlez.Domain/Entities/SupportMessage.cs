using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class SupportMessage : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid SenderId { get; set; }
    public bool IsFromSupport { get; set; }
    public string Content { get; set; } = string.Empty;

    public SupportTicket Ticket { get; set; } = null!;
}
