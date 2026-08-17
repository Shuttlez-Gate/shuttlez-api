using Shuttlez.Application.Admin.DTOs;

namespace Shuttlez.Application.Admin.Services;

/// <summary>يوزّع مقاعد الطلب على الأسطول المتاح حسب نوع المركبة مع ترحيل العجز للسعة الأكبر.</summary>
public static class SeatDemandDistributor
{
    private static readonly string[] EscalationOrder = ["carshuttle", "minibus", "bus"];

    public static IReadOnlyList<SeatAssignmentDto> Distribute(
        IReadOnlyDictionary<string, int> seatsNeededByType,
        IReadOnlyList<FleetAvailabilityDto> fleet)
    {
        var remainingCapacity = fleet.ToDictionary(
            f => f.VehicleType,
            f => f.Vehicles.Select(v => (v.VehicleId, v.Capacity, Remaining: v.Capacity)).ToList(),
            StringComparer.OrdinalIgnoreCase);

        var result = new List<SeatAssignmentDto>();

        foreach (var preferred in EscalationOrder)
        {
            if (!seatsNeededByType.TryGetValue(preferred, out var needed) || needed <= 0)
            {
                continue;
            }

            var leftover = needed;
            var covered = 0;
            var assignedType = preferred;
            var usedVehicleIds = new List<Guid>();
            var capacityPerVehicle = 0;
            string? note = null;

            // حاول النوع المفضّل ثم الأكبر.
            foreach (var candidate in EscalationPath(preferred))
            {
                if (!remainingCapacity.TryGetValue(candidate, out var pool) || pool.Count == 0)
                {
                    continue;
                }

                var vehiclesNeeded = 0;
                while (leftover > 0)
                {
                    var slot = pool
                        .Where(p => p.Remaining > 0)
                        .OrderByDescending(p => p.Remaining)
                        .FirstOrDefault();

                    if (slot.VehicleId == Guid.Empty || slot.Remaining <= 0)
                    {
                        break;
                    }

                    var take = Math.Min(leftover, slot.Remaining);
                    leftover -= take;
                    covered += take;
                    usedVehicleIds.Add(slot.VehicleId);
                    vehiclesNeeded++;
                    capacityPerVehicle = Math.Max(capacityPerVehicle, slot.Capacity);

                    var idx = pool.FindIndex(p => p.VehicleId == slot.VehicleId);
                    if (idx >= 0)
                    {
                        var updated = pool[idx];
                        pool[idx] = updated with { Remaining = updated.Remaining - take };
                    }

                    assignedType = candidate;
                    if (candidate != preferred)
                    {
                        note = $"تم توجيه جزء من طلبات {preferred} إلى {candidate} لعدم كفاية الأسطول";
                    }
                }

                remainingCapacity[candidate] = pool;

                if (leftover <= 0)
                {
                    result.Add(new SeatAssignmentDto(
                        preferred,
                        needed,
                        covered,
                        0,
                        assignedType,
                        usedVehicleIds.Distinct().Count(),
                        capacityPerVehicle,
                        usedVehicleIds.Distinct().ToList(),
                        note));
                    leftover = 0;
                    break;
                }
            }

            if (leftover > 0)
            {
                result.Add(new SeatAssignmentDto(
                    preferred,
                    needed,
                    covered,
                    leftover,
                    assignedType,
                    usedVehicleIds.Distinct().Count(),
                    capacityPerVehicle,
                    usedVehicleIds.Distinct().ToList(),
                    note ?? "عجز في الأسطول — أضف مركبات أو خفّض الطلب"));
            }
        }

        // أنواع غير معروفة في القائمة.
        foreach (var extra in seatsNeededByType.Keys.Except(EscalationOrder, StringComparer.OrdinalIgnoreCase))
        {
            var needed = seatsNeededByType[extra];
            result.Add(new SeatAssignmentDto(
                extra,
                needed,
                0,
                needed,
                extra,
                0,
                0,
                [],
                "نوع مركبة غير مدعوم"));
        }

        return result;
    }

    public static IReadOnlyList<ProposedTripDto> BuildProposedTrips(
        IReadOnlyList<SeatAssignmentDto> assignments,
        IReadOnlyList<FleetAvailabilityDto> fleet)
    {
        var vehicleLookup = fleet
            .SelectMany(f => f.Vehicles.Select(v => (Type: f.VehicleType, Vehicle: v)))
            .ToDictionary(x => x.Vehicle.VehicleId, x => x);

        var trips = new List<ProposedTripDto>();

        foreach (var assignment in assignments)
        {
            if (assignment.SeatsCovered <= 0 || assignment.VehicleIds.Count == 0)
            {
                continue;
            }

            var remainingSeats = assignment.SeatsCovered;
            foreach (var vehicleId in assignment.VehicleIds)
            {
                if (!vehicleLookup.TryGetValue(vehicleId, out var entry))
                {
                    continue;
                }

                var seats = Math.Min(remainingSeats, entry.Vehicle.Capacity);
                if (seats <= 0)
                {
                    continue;
                }

                trips.Add(new ProposedTripDto(
                    entry.Type,
                    entry.Vehicle.VehicleId,
                    entry.Vehicle.PlateNumber,
                    entry.Vehicle.DriverId,
                    entry.Vehicle.DriverName,
                    seats,
                    SuggestedPricePerSeat: 100m));

                remainingSeats -= seats;
                if (remainingSeats <= 0)
                {
                    break;
                }
            }
        }

        return trips;
    }

    private static IEnumerable<string> EscalationPath(string preferred)
    {
        yield return preferred;
        foreach (var type in EscalationOrder)
        {
            if (!string.Equals(type, preferred, StringComparison.OrdinalIgnoreCase))
            {
                yield return type;
            }
        }
    }
}
