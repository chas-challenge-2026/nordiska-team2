using Microsoft.AspNetCore.Mvc;

namespace NordiskaPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAccounts()
    {
        throw new NotImplementedException();
    }
}