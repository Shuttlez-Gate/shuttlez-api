using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class SavedLocation : BaseEntity
{
    public Guid UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsFavorite { get; set; }

    public User User { get; set; } = null!;
}
