using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace NordiskaPortal.Api.Controllers;

// TEMPORARY: exists only to verify the rate limiter works end-to-end.
// Delete this once a real sensitive endpoint (login, deposit/withdrawal)
// exists and can carry the [EnableRateLimiting] attribute instead.
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("SensitiveEndpoints")]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("pong");
}