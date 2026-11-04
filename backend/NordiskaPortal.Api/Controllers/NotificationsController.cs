using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.Extensions;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private const int MaxLimit = 100;

    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // GET /api/notifications?limit=50
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);

        var customerId = User.GetCustomerId();
        var notifications = await _notificationService.GetNotificationsForCustomerAsync(customerId, limit);
        return Ok(notifications);
    }
}

