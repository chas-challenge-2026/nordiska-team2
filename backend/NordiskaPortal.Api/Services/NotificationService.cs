using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services;

public class NotificationService : INotificationService
{
    private readonly BankContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(BankContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnqueueTransactionNotificationAsync(
        int customerId,
        string recipient,
        string type,
        string refId,
        string message)
    {
        // Idempotency check: don't create duplicate notifications for the same transaction/reference
        var exists = await _db.Notifications
            .AnyAsync(n => n.RefId == refId && n.Type == type);

        if (exists)
        {
            _logger.LogInformation("Notification already enqueued for refId {RefId} and type {Type}. Skipping duplicate.", refId, type);
            return;
        }

        var notification = new Notification
        {
            CustomerId = customerId,
            Recipient = recipient,
            Type = type,
            RefId = refId,
            Message = message,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Enqueued notification {NotificationId} for customer {CustomerId} (type: {Type}).", notification.Id, customerId, type);
    }

    public async Task<List<NotificationDto>> GetNotificationsForCustomerAsync(int customerId)
    {
        return await _db.Notifications
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Message,
                n.Status,
                n.CreatedAt,
                n.SentAt
            ))
            .ToListAsync();
    }
}
