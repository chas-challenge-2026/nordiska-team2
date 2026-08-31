using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("SensitiveEndpoints")]
    public IActionResult Login(LoginRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        throw new NotImplementedException();
    }
}