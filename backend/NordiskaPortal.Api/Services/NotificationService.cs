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

    public async Task StageTransactionNotificationAsync(
        int customerId,
        string recipient,
        string type,
        string refId,
        string message)
    {
        // Idempotency: don't create duplicates for the same reference.
        // Checks both saved rows and rows already staged on this context.
        var alreadyStaged = _db.Notifications.Local.Any(n => n.RefId == refId && n.Type == type);
        if (alreadyStaged || await _db.Notifications.AnyAsync(n => n.RefId == refId && n.Type == type))
        {
            _logger.LogInformation("Notification already exists for refId {RefId} and type {Type}. Skipping duplicate.", refId, type);
            return;
        }

        // Intentionally NOT saved here. The caller's SaveChangesAsync commits it
        // together with the transaction, so they are one unit of work.
        _db.Notifications.Add(new Notification
        {
            CustomerId = customerId,
            Recipient = recipient,
            Type = type,
            RefId = refId,
            Message = message,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        });
    }

    public async Task<List<NotificationDto>> GetNotificationsForCustomerAsync(int customerId, int limit = 50)
    {
        var notifications = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var txIds = notifications
            .Select(n => int.TryParse(n.RefId, out var id) ? (int?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var txLookup = txIds.Count == 0
            ? new Dictionary<int, (decimal Amount, string? AccountNumber, string? CounterpartAccountNumber)>()
            : (await _db.Transactions
                .AsNoTracking()
                .Where(t => txIds.Contains(t.Id))
                .Select(t => new
                {
                    t.Id,
                    t.Amount,
                    AccountNumber = _db.SavingsAccounts
                        .Where(a => a.Id == t.AccountId)
                        .Select(a => a.AccountNumber)
                        .FirstOrDefault(),
                    t.CounterpartAccountNumber
                })
                .ToListAsync())
                .ToDictionary(x => x.Id, x => (x.Amount, x.AccountNumber, x.CounterpartAccountNumber));

        return notifications
            .Select(n =>
            {
                if (int.TryParse(n.RefId, out var txId) && txLookup.TryGetValue(txId, out var tx))
                {
                    return new NotificationDto(
                        n.Id,
                        n.Type,
                        n.RefId,
                        n.Message,
                        n.Status,
                        n.CreatedAt,
                        n.SentAt,
                        tx.Amount,
                        tx.AccountNumber,
                        tx.CounterpartAccountNumber
                    );
                }

                return new NotificationDto(
                    n.Id,
                    n.Type,
                    n.RefId,
                    n.Message,
                    n.Status,
                    n.CreatedAt,
                    n.SentAt
                );
            })
            .ToList();
    }
}
