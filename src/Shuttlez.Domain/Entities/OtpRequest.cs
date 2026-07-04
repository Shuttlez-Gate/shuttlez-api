using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

public class OtpRequest : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool IsUsed { get; set; }
}
