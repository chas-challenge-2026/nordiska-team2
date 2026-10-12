using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Services;

public interface INotificationService
{
    Task EnqueueTransactionNotificationAsync(int customerId, string recipient, string type, string refId, string message);
    Task<List<NotificationDto>> GetNotificationsForCustomerAsync(int customerId);
}
