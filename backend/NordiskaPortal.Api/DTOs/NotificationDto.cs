namespace NordiskaPortal.Api.DTOs;

public record NotificationDto(
    int Id,
    string Type,
    string RefId,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime? SentAt,
    decimal? Amount = null,
    string? AccountNumber = null,
    string? CounterpartAccountNumber = null
);
