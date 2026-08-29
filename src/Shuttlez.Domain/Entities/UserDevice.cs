using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

/// <summary>FCM (or equivalent) device registration for push — multi-device per user.</summary>
public class UserDevice : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAt { get; set; }

    public User User { get; set; } = null!;
}
