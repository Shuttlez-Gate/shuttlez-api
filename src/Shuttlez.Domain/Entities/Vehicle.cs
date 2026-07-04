using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

public class Vehicle : BaseEntity
{
    public string PlateNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public VehicleType Type { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Driver> Drivers { get; set; } = [];
}
