using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class GroupMember : BaseEntity
{
    public Guid GroupRequestId { get; set; }
    public Guid UserId { get; set; }
    public bool IsOrganizer { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public GroupRequest GroupRequest { get; set; } = null!;
    public User User { get; set; } = null!;
}
