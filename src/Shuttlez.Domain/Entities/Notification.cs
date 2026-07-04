using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public User User { get; set; } = null!;
}
