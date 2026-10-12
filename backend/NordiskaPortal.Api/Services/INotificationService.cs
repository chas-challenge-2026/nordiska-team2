using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Services;

public interface INotificationService
{
    /// <summary>
    /// Stages a pending notification on the shared DbContext WITHOUT saving it.
    /// The caller saves it together with the business change (same unit of work),
    /// so a transaction and its notification either both exist or neither does.
    /// Staging the same (type, refId) twice is a no-op.
    /// </summary>
    Task StageTransactionNotificationAsync(int customerId, string recipient, string type, string refId, string message);

    Task<List<NotificationDto>> GetNotificationsForCustomerAsync(int customerId);
}
