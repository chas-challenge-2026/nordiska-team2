using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.Extensions;
using NordiskaPortal.Api.Services;

// GET /api/customers/me 
// Profile info for the LOGGED-IN customer only.
// Same pattern as AccountsController: identity from the token.
namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var customerId = User.GetCustomerId();
            var customer = await _customerService.GetCustomerAsync(customerId);

            if (customer == null)
                return NotFound();

            return Ok(customer);
        }
    }
}