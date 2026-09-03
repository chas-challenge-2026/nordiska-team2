using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.Services;

// Returns a customer's accounts + balances for the dashboard.

// TODO: customerId comes straight from the  URL and
//       will need a check that the caller actually 
//       IS that customer.

namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("{customerId:int}")]
        public async Task<IActionResult> GetAccountsForCustomer(int customerId)
        {
            var accounts = await _accountService.GetAccountsForCustomerAsync(customerId);
            return Ok(accounts);
        }
    }
}