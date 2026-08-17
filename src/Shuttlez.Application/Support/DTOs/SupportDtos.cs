using Shuttlez.Application.Trips.DTOs;

namespace Shuttlez.Application.Support.DTOs;

public record SupportTicketDto(
    Guid Id,
    string Tab,
    string ReportStatus,
    string Subject,
    string? LastMessage,
    TripListItemDto? Trip,
    string SeatsLabel,
    string PaymentLabel);

public record SupportMessageDto(
    Guid Id,
    bool IsFromSupport,
    string Content,
    string Time);

public record CreateSupportTicketRequest(
    string? Subject,
    Guid? TripId,
    string? InitialMessage);

public record CreateSupportTicketResponse(
    Guid TicketId,
    string Message);

public record SendSupportMessageRequest(string Content);

public record SendSupportMessageResponse(
    Guid MessageId,
    string Message);
