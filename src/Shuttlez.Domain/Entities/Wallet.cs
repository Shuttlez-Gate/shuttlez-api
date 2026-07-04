using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class Wallet : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "EGP";

    public User User { get; set; } = null!;
    public ICollection<WalletTransaction> Transactions { get; set; } = [];
}
