using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class WalletTransaction : BaseEntity
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Reference { get; set; }

    public Wallet Wallet { get; set; } = null!;
}
