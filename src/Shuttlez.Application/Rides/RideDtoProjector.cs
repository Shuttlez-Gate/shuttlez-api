using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Rides.DTOs;

namespace Shuttlez.Application.Rides;

/// <summary>
/// Shared Ride → RideDto projection. Captain card fields come only from existing Driver/User/Vehicle data.
/// No ETA. Rating omitted when RatingCount == 0 (avoid fake zeros looking like reviews).
/// </summary>
public static class RideDtoProjector
{
    public static async Task<RideDto> ProjectAsync(
        IAppDbContext db,
        Guid rideId,
        CancellationToken ct)
    {
        var row = await db.RideRequests
            .AsNoTracking()
            .Where(r => r.Id == rideId && !r.IsDeleted)
            .Select(r => new
            {
                r.Id,
                r.RiderUserId,
                r.PickupLatitude,
                r.PickupLongitude,
                r.PickupAddress,
                r.DestinationLatitude,
                r.DestinationLongitude,
                r.DestinationAddress,
                r.FromZoneKey,
                r.ToZoneKey,
                r.RideFareRuleId,
                r.Status,
                r.FareAmount,
                r.CommissionRate,
                r.CommissionAmount,
                r.CaptainEarnings,
                r.TotalAmount,
                r.PaymentMethod,
                r.IsCashConfirmed,
                r.DriverId,
                DriverName = r.Driver == null
                    ? null
                    : (r.Driver.User.FullName ?? r.Driver.User.Phone),
                DriverPhone = r.Driver == null ? null : r.Driver.User.Phone,
                DriverPhotoUrl = r.Driver == null ? null : r.Driver.User.AvatarUrl,
                DriverRatingAverage = r.Driver == null ? (decimal?)null : r.Driver.RatingAverage,
                DriverRatingCount = r.Driver == null ? (int?)null : r.Driver.RatingCount,
                VehicleKind = r.Driver == null
                    ? null
                    : (r.Driver.VehicleKind ??
                       (r.Driver.Vehicle == null ? null : r.Driver.Vehicle.Type.ToString())),
                VehicleModel = r.Driver == null
                    ? null
                    : (r.Driver.VehicleModelName ??
                       (r.Driver.Vehicle == null ? null : r.Driver.Vehicle.Model)),
                VehicleColor = r.Driver == null ? null : r.Driver.VehicleColor,
                PlateNumber = r.Driver == null
                    ? null
                    : (r.Driver.PlateNumber ??
                       (r.Driver.Vehicle == null ? null : r.Driver.Vehicle.PlateNumber)),
                r.DistanceKm,
                r.BaseFareApplied,
                r.PricePerKmApplied,
                r.CaptainLatitude,
                r.CaptainLongitude,
                r.CaptainLocationUpdatedAt,
                r.AssignedAt,
                r.StartedAt,
                r.CompletedAt,
                r.CancelledAt,
                r.ReferenceCode,
                r.CreatedAt
            })
            .FirstAsync(ct);

        var ratingCount = row.DriverRatingCount is > 0 ? row.DriverRatingCount : null;
        var ratingAverage = ratingCount is > 0 ? row.DriverRatingAverage : null;
        var driverPhone = row.DriverId is not null ? TrimOrNull(row.DriverPhone) : null;

        // Location only while live — never after Completed/Cancelled.
        var exposeLocation =
            row.Status is Domain.Enums.RideRequestStatus.Assigned
                or Domain.Enums.RideRequestStatus.InProgress;

        return new RideDto(
            row.Id,
            row.RiderUserId,
            row.PickupLatitude,
            row.PickupLongitude,
            row.PickupAddress,
            row.DestinationLatitude,
            row.DestinationLongitude,
            row.DestinationAddress,
            row.FromZoneKey,
            row.ToZoneKey,
            row.RideFareRuleId,
            row.Status.ToString(),
            row.FareAmount,
            row.CommissionRate,
            row.CommissionAmount,
            row.CaptainEarnings,
            row.TotalAmount,
            row.PaymentMethod,
            row.IsCashConfirmed,
            row.DriverId,
            TrimOrNull(row.DriverName),
            row.AssignedAt,
            row.StartedAt,
            row.CompletedAt,
            row.CancelledAt,
            row.ReferenceCode,
            row.CreatedAt,
            TrimOrNull(row.DriverPhotoUrl),
            ratingAverage,
            ratingCount,
            TrimOrNull(row.VehicleKind),
            TrimOrNull(row.VehicleModel),
            TrimOrNull(row.VehicleColor),
            TrimOrNull(row.PlateNumber),
            driverPhone,
            row.DistanceKm,
            row.BaseFareApplied,
            row.PricePerKmApplied,
            exposeLocation ? row.CaptainLatitude : null,
            exposeLocation ? row.CaptainLongitude : null,
            exposeLocation ? row.CaptainLocationUpdatedAt : null);
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
