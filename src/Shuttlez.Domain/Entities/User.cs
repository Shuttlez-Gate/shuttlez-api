using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

public class User : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public Gender? Gender { get; set; }
    public string? AvatarUrl { get; set; }
    public UserType UserType { get; set; } = UserType.Passenger;
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public string? FcmToken { get; set; }
    public bool IsActive { get; set; } = true;

    public Driver? Driver { get; set; }
    public ICollection<SavedLocation> SavedLocations { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public Wallet? Wallet { get; set; }
}
