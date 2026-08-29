namespace Shuttlez.Application.Admin.DTOs;

// ── Auth ─────────────────────────────────────────────────────────────────────

public record AdminLoginRequest(string Phone, string Code);

public record AdminIdentityDto(
    Guid Id,
    string Phone,
    string? FullName,
    string? Email,
    string? AvatarUrl,
    string Role);

public record AdminLoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    AdminIdentityDto Admin);

// ── Dashboard ────────────────────────────────────────────────────────────────

public record MetricDto(
    string Key,
    string Label,
    decimal Value,
    decimal? PreviousValue,
    string Format);

public record TimeSeriesPointDto(DateTime Date, decimal Value);

public record TimeSeriesDto(string Key, string Label, IReadOnlyList<TimeSeriesPointDto> Points);

public record BreakdownSliceDto(string Label, decimal Value);

public record TopRouteDto(
    Guid RouteId,
    string Name,
    int TripCount,
    int BookingCount,
    decimal Revenue);

public record ActivityItemDto(
    string Type,
    string Title,
    string? Subtitle,
    DateTime At);

public record AdminDashboardDto(
    IReadOnlyList<MetricDto> Metrics,
    IReadOnlyList<TimeSeriesDto> Series,
    IReadOnlyList<BreakdownSliceDto> TripStatusBreakdown,
    IReadOnlyList<BreakdownSliceDto> BookingStatusBreakdown,
    IReadOnlyList<TopRouteDto> TopRoutes,
    IReadOnlyList<ActivityItemDto> RecentActivity);

// ── Users ────────────────────────────────────────────────────────────────────

public record AdminUserDto(
    Guid Id,
    string Phone,
    string? FullName,
    string? Email,
    string? Gender,
    string? AvatarUrl,
    string UserType,
    decimal RatingAverage,
    int RatingCount,
    bool IsActive,
    decimal WalletBalance,
    int BookingCount,
    DateTime CreatedAt);

public record UpdateAdminUserRequest(
    string? FullName,
    string? Email,
    string? Gender,
    string? AvatarUrl,
    string? UserType,
    bool? IsActive);

public record CreateAdminUserRequest(
    string Phone,
    string? FullName,
    string? Email,
    string? Gender,
    string UserType = "passenger");

public record AdjustWalletRequest(decimal Amount, string? Description);

// ── Fleet ────────────────────────────────────────────────────────────────────

public record AdminVehicleDto(
    Guid Id,
    string PlateNumber,
    string Model,
    string Type,
    int Capacity,
    bool IsActive,
    int DriverCount,
    DateTime CreatedAt);

public record SaveVehicleRequest(
    string PlateNumber,
    string Model,
    string Type,
    int Capacity,
    bool IsActive = true);

public record AdminDriverDto(
    Guid Id,
    Guid UserId,
    string Phone,
    string? FullName,
    Guid? VehicleId,
    string? VehiclePlate,
    string? VehicleModel,
    decimal RatingAverage,
    int RatingCount,
    bool IsOnline,
    bool IsActive,
    string VerificationStatus,
    int DocumentCount,
    int TripCount,
    DateTime CreatedAt);

public record AdminDriverDocumentDto(
    Guid Id,
    string DocumentType,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Url,
    string UploadedBy,
    string? Notes,
    DateTime CreatedAt);

public record AdminDriverDetailsDto(
    Guid Id,
    Guid UserId,
    string Phone,
    string? FullName,
    string? Email,
    string? Gender,
    string? AvatarUrl,
    Guid? VehicleId,
    string? VehiclePlate,
    string? VehicleModel,
    decimal RatingAverage,
    int RatingCount,
    bool IsOnline,
    bool IsActive,
    string VerificationStatus,
    string? NationalId,
    string? BirthDate,
    string? LicenseNumber,
    string? LicenseType,
    string? LicenseExpiry,
    string? VehicleKind,
    string? VehicleModelName,
    int? ManufactureYear,
    string? PlateNumber,
    string? VehicleColor,
    int? Seats,
    string? AdminNotes,
    DateTime? VerifiedAt,
    int TripCount,
    DateTime CreatedAt,
    IReadOnlyList<AdminDriverDocumentDto> Documents);

public record CreateDriverRequest(
    string Phone,
    string? FullName = null,
    Guid? VehicleId = null,
    string? Email = null,
    string? Gender = null,
    string? VerificationStatus = null,
    bool? IsActive = null,
    bool? IsOnline = null,
    string? NationalId = null,
    string? BirthDate = null,
    string? LicenseNumber = null,
    string? LicenseType = null,
    string? LicenseExpiry = null,
    string? VehicleKind = null,
    string? VehicleModelName = null,
    int? ManufactureYear = null,
    string? PlateNumber = null,
    string? VehicleColor = null,
    int? Seats = null,
    string? AdminNotes = null);

public record UpdateDriverRequest(
    Guid? VehicleId,
    bool? IsOnline,
    bool? IsActive,
    string? FullName,
    string? Email,
    string? Gender,
    string? VerificationStatus,
    string? NationalId,
    string? BirthDate,
    string? LicenseNumber,
    string? LicenseType,
    string? LicenseExpiry,
    string? VehicleKind,
    string? VehicleModelName,
    int? ManufactureYear,
    string? PlateNumber,
    string? VehicleColor,
    int? Seats,
    string? AdminNotes);

// ── Routes ───────────────────────────────────────────────────────────────────

public record AdminStopDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    int Order);

public record AdminRouteDto(
    Guid Id,
    string Name,
    string? Description,
    double StartLatitude,
    double StartLongitude,
    double EndLatitude,
    double EndLongitude,
    string? EncodedPolyline,
    int? DistanceMeters,
    int? DurationSeconds,
    bool IsActive,
    int StopCount,
    int TripCount,
    DateTime CreatedAt);

public record AdminRouteDetailsDto(
    AdminRouteDto Route,
    IReadOnlyList<AdminStopDto> Stops);

public record SaveRouteRequest(
    string Name,
    string? Description,
    double StartLatitude,
    double StartLongitude,
    double EndLatitude,
    double EndLongitude,
    string? EncodedPolyline,
    int? DistanceMeters,
    int? DurationSeconds,
    bool IsActive = true);

public record SaveStopRequest(
    string Name,
    double Latitude,
    double Longitude,
    int Order);

// ── Trips & bookings ─────────────────────────────────────────────────────────

public record AdminTripDto(
    Guid Id,
    Guid RouteId,
    string RouteName,
    Guid? DriverId,
    string? DriverName,
    string Status,
    DateTime ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    decimal PricePerSeat,
    int AvailableSeats,
    string? ReferenceCode,
    int BookingCount,
    decimal Revenue,
    DateTime CreatedAt);

public record SaveTripRequest(
    Guid RouteId,
    Guid? DriverId,
    DateTime ScheduledAt,
    decimal PricePerSeat,
    int AvailableSeats,
    string? Status,
    string? ReferenceCode);

public record AdminBookingDto(
    Guid Id,
    Guid TripId,
    string RouteName,
    DateTime ScheduledAt,
    Guid UserId,
    string UserPhone,
    string? UserName,
    string Status,
    int SeatCount,
    decimal TotalAmount,
    string PaymentMethod,
    string? ReferenceCode,
    string? InvoiceStatus,
    DateTime CreatedAt,
    decimal PricePerSeat = 0,
    decimal CommissionRate = 0,
    decimal CommissionAmount = 0,
    decimal CaptainEarnings = 0);

public record UpdateBookingStatusRequest(string Status);

// ── Requests & leads ─────────────────────────────────────────────────────────

public record AdminRouteRequestDto(
    Guid Id,
    Guid UserId,
    string UserPhone,
    string? UserName,
    string FromAddress,
    string ToAddress,
    string Status,
    string? FromTime,
    string? ToTime,
    int? WeeklyCount,
    string? UsageDays,
    string? UsageReason,
    string PreferredVehicleType,
    double FromLatitude,
    double FromLongitude,
    double ToLatitude,
    double ToLongitude,
    DateTime CreatedAt);

public record UpdateRouteRequestStatusRequest(string Status, string? AdminNote);

public record AdminLandingLeadDto(
    Guid Id,
    string Kind,
    string Phone,
    string? FullName,
    string? From,
    string? To,
    string? FromTime,
    string? ToTime,
    int? WeeklyCount,
    string? UsageDays,
    string? UsageReason,
    string? VehicleType,
    string? Notes,
    string Source,
    DateTime CreatedAt);

// ── Content ──────────────────────────────────────────────────────────────────

public record AdminFaqDto(
    Guid Id,
    string Question,
    string Answer,
    int Order,
    bool IsActive);

public record SaveFaqRequest(string Question, string Answer, int Order, bool IsActive = true);

public record AdminLegalDto(
    Guid Id,
    string Slug,
    string Title,
    string Content,
    string? TitleEn,
    string? ContentEn,
    bool IsActive,
    DateTime? UpdatedAt);

public record SaveLegalRequest(
    string Slug,
    string Title,
    string Content,
    string? TitleEn,
    string? ContentEn,
    bool IsActive = true);

public record AdminPackageDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int TripCount,
    int ValidityDays,
    bool IsActive);

public record SavePackageRequest(
    string Name,
    string? Description,
    decimal Price,
    int TripCount,
    int ValidityDays,
    bool IsActive = true);

// ── Support & notifications ──────────────────────────────────────────────────

public record AdminSupportTicketDto(
    Guid Id,
    Guid UserId,
    string UserPhone,
    string? UserName,
    Guid? TripId,
    string Subject,
    string Status,
    int MessageCount,
    string? LastMessage,
    DateTime? LastMessageAt,
    DateTime? ClosedAt,
    DateTime CreatedAt);

public record AdminSupportMessageDto(
    Guid Id,
    Guid TicketId,
    bool IsFromSupport,
    string Content,
    DateTime CreatedAt);

public record ReplyTicketRequest(string Content);

public record UpdateTicketStatusRequest(string Status);

public record AdminNotificationDto(
    Guid Id,
    Guid UserId,
    string UserPhone,
    string Title,
    string Body,
    string? Type,
    bool IsRead,
    DateTime CreatedAt);

/// <summary>بث إشعار لمجموعة مستخدمين: all / passengers / drivers / محدد.</summary>
public record BroadcastNotificationRequest(
    string Title,
    string Body,
    string Audience = "all",
    IReadOnlyList<Guid>? UserIds = null,
    string? Type = "admin");

public record BroadcastResultDto(int Sent);

// ── Reviews ──────────────────────────────────────────────────────────────────

public record AdminReviewDto(
    Guid Id,
    Guid TripId,
    string RouteName,
    Guid UserId,
    string UserPhone,
    string? UserName,
    Guid? DriverId,
    string? DriverName,
    int Stars,
    string? Comment,
    DateTime CreatedAt);
