using System.Text.Json;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Common;
using Shuttlez.Domain.Entities;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Admin;

/// <summary>تحويلات مشتركة بين هاندلرز لوحة التحكم + تحليل حقول النصوص الحرة.</summary>
public static class AdminMapper
{
    // ── Enums ↔ نصوص الواجهة ────────────────────────────────────────────────

    public static string ToSlug(this Enum value) => value.ToString().ToLowerInvariant();

    public static UserType ParseUserType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "admin" => UserType.Admin,
        "driver" => UserType.Driver,
        "passenger" => UserType.Passenger,
        _ => throw new AppException("نوع مستخدم غير معروف")
    };

    public static UserType? ParseUserTypeOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseUserType(value);

    public static Gender? ParseGender(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "male" or "ذكر" => Gender.Male,
        "female" or "أنثى" => Gender.Female,
        _ => null
    };

    public static string? GenderSlug(Gender? gender) => gender switch
    {
        Gender.Male => "male",
        Gender.Female => "female",
        _ => null
    };

    public static VehicleType ParseVehicleType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "carshuttle" or "car_shuttle" or "car" => VehicleType.CarShuttle,
        "minibus" or "mini_bus" => VehicleType.MiniBus,
        "bus" => VehicleType.Bus,
        _ => throw new AppException("نوع مركبة غير معروف")
    };

    public static TripStatus ParseTripStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "scheduled" => TripStatus.Scheduled,
        "driverassigned" or "driver_assigned" => TripStatus.DriverAssigned,
        "inprogress" or "in_progress" => TripStatus.InProgress,
        "completed" => TripStatus.Completed,
        "cancelled" or "canceled" => TripStatus.Cancelled,
        _ => throw new AppException("حالة رحلة غير معروفة")
    };

    public static BookingStatus ParseBookingStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "pending" => BookingStatus.Pending,
        "confirmed" => BookingStatus.Confirmed,
        "cancelled" or "canceled" => BookingStatus.Cancelled,
        "expired" => BookingStatus.Expired,
        _ => throw new AppException("حالة حجز غير معروفة")
    };

    // ── Notes JSON لطلبات خطوط السير ────────────────────────────────────────

    public record RouteRequestNotes(
        string? FromTime,
        string? ToTime,
        int? WeeklyCount,
        string? UsageDays,
        string? UsageReason,
        string? AdminNote);

    public static RouteRequestNotes ParseRouteRequestNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return new RouteRequestNotes(null, null, null, null, null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(notes);
            var root = doc.RootElement;
            return new RouteRequestNotes(
                ReadString(root, "FromTime", "fromTime"),
                ReadString(root, "ToTime", "toTime"),
                ReadInt(root, "WeeklyCount", "weeklyCount"),
                ReadString(root, "UsageDays", "usageDays"),
                ReadString(root, "UsageReason", "usageReason"),
                ReadString(root, "AdminNote", "adminNote"));
        }
        catch (JsonException)
        {
            return new RouteRequestNotes(null, null, null, null, null, notes);
        }
    }

    public static string SerializeRouteRequestNotes(RouteRequestNotes notes) =>
        JsonSerializer.Serialize(new
        {
            notes.FromTime,
            notes.ToTime,
            notes.WeeklyCount,
            notes.UsageDays,
            notes.UsageReason,
            notes.AdminNote
        });

    private static string? ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
        }
        return null;
    }

    private static int? ReadInt(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetInt32();
            }
        }
        return null;
    }

    // ── DTO builders ────────────────────────────────────────────────────────

    public static AdminStopDto ToDto(this Stop stop) =>
        new(stop.Id, stop.Name, stop.Latitude, stop.Longitude, stop.Order);

    public static AdminFaqDto ToDto(this FaqItem item) =>
        new(item.Id, item.Question, item.Answer, item.Order, item.IsActive);

    public static AdminLegalDto ToDto(this LegalDocument doc) =>
        new(doc.Id, doc.Slug, doc.Title, doc.Content, doc.IsActive, doc.UpdatedAt);

    public static AdminPackageDto ToDto(this SubscriptionPackage package) =>
        new(package.Id, package.Name, package.Description, package.Price,
            package.TripCount, package.ValidityDays, package.IsActive);

    public static AdminSupportMessageDto ToDto(this SupportMessage message) =>
        new(message.Id, message.TicketId, message.IsFromSupport, message.Content, message.CreatedAt);
}
