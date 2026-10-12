namespace NordiskaPortal.Api.DTOs;

public record NotificationDto(
    int Id,
    string Type,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime? SentAt
);
