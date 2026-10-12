using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;

namespace NordiskaPortal.Api.Services;

public class NotificationBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationBackgroundWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public const int MaxRetries = 3;

    // Exponential backoff: 30s after the 1st failure, 2 min after the 2nd.
    // The 3rd failure marks the notification as permanently failed.
    public static TimeSpan BackoffFor(int retryCount) =>
        TimeSpan.FromSeconds(30 * Math.Pow(4, Math.Max(0, retryCount - 1)));

    public NotificationBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationBackgroundWorker> logger,
        TimeSpan? pollInterval = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationBackgroundWorker started with poll interval {Interval}.", _pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingNotificationsAsync(stoppingToken);
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in NotificationBackgroundWorker processing cycle.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("NotificationBackgroundWorker stopping.");
    }

    public async Task<int> ProcessPendingNotificationsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankContext>();

        var now = DateTime.UtcNow;
        var pending = await db.Notifications
            .Where(n => n.Status == "pending"
                        && n.RetryCount < MaxRetries
                        && (n.NextAttemptAt == null || n.NextAttemptAt <= now))
            .OrderBy(n => n.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return 0;

        _logger.LogInformation("Processing {Count} pending notification(s).", pending.Count);

        foreach (var notification in pending)
        {
            try
            {
                // In v2, outbound SMTP/messaging is decoupled from the user request thread.
                // We dispatch or simulate dispatching the email safely.
                await DispatchEmailAsync(notification.Recipient, notification.Type, notification.Message, cancellationToken);

                notification.Status = "sent";
                notification.SentAt = DateTime.UtcNow;
                _logger.LogInformation("Notification {Id} sent to {Recipient}.", notification.Id, notification.Recipient);
            }
            catch (Exception ex)
            {
                notification.RetryCount++;
                if (notification.RetryCount >= MaxRetries)
                {
                    notification.Status = "failed";
                    notification.NextAttemptAt = null;
                    _logger.LogError(ex, "Notification {Id} failed permanently after {Retries} attempts.", notification.Id, notification.RetryCount);
                }
                else
                {
                    var delay = BackoffFor(notification.RetryCount);
                    notification.NextAttemptAt = DateTime.UtcNow + delay;
                    _logger.LogWarning(ex, "Notification {Id} attempt {Retries} failed, will retry in {Delay}.", notification.Id, notification.RetryCount, delay);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return pending.Count;
    }

    protected virtual Task DispatchEmailAsync(string recipient, string type, string message, CancellationToken cancellationToken)
    {
        // Outbound dispatch simulation (decoupled from caller latency).
        // If an SMTP provider or mail client is configured in future, it hooks in here without blocking user requests.
        return Task.CompletedTask;
    }
}
