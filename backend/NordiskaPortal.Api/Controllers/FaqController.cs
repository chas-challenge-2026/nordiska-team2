using Microsoft.AspNetCore.Mvc;

namespace NordiskaPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaqController : ControllerBase
{
    [HttpGet("search")]
    public IActionResult Search([FromQuery] string q)
    {
        throw new NotImplementedException();
    }
}