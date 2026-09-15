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
    }
}