using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

public class Driver : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? VehicleId { get; set; }
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public bool IsOnline { get; set; }
    public bool IsActive { get; set; } = true;

    public DriverVerificationStatus VerificationStatus { get; set; } =
        DriverVerificationStatus.Approved;

    public string? NationalId { get; set; }
    public DateOnly? BirthDate { get; set; }

    public string? LicenseNumber { get; set; }
    public string? LicenseType { get; set; }
    public DateOnly? LicenseExpiry { get; set; }

    public string? VehicleKind { get; set; }
    public string? VehicleModelName { get; set; }
    public int? ManufactureYear { get; set; }
    public string? PlateNumber { get; set; }
    public string? VehicleColor { get; set; }
    public int? Seats { get; set; }

    public string? AdminNotes { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public User User { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
    public ICollection<Trip> Trips { get; set; } = [];
    public ICollection<DriverDocument> Documents { get; set; } = [];
}
