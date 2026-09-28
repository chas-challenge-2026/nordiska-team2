using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.Extensions;
using NordiskaPortal.Api.Services;

// Returns a customer's accounts + balances for the dashboard.
namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;
 
        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }
 
        [HttpGet]
        public async Task<IActionResult> GetMyAccounts()
        {
            var customerId = User.GetCustomerId();
            var accounts = await _accountService.GetAccountsForCustomerAsync(customerId);
            return Ok(accounts);
        }

        [HttpGet("financial-summary")]
        public async Task<IActionResult> GetFinancialSummary(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var customerId = User.GetCustomerId();

            var periodStart = from
                ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = to ?? DateTime.UtcNow;

            // PostgreSQL kräver UTC — query string-datum kommer in som Kind=Unspecified
            if (periodStart.Kind == DateTimeKind.Unspecified)
                periodStart = DateTime.SpecifyKind(periodStart, DateTimeKind.Utc);
            if (periodEnd.Kind == DateTimeKind.Unspecified)
                periodEnd = DateTime.SpecifyKind(periodEnd, DateTimeKind.Utc);

            var summary = await _accountService.GetFinancialSummaryAsync(customerId, periodStart, periodEnd);
            return Ok(summary);
        }
    }
}