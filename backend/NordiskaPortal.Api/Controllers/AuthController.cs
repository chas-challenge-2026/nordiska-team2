using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const string RefreshCookieName = "refreshToken";
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [EnableRateLimiting("SensitiveEndpoints")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);
            if (result is null)
                return Unauthorized(new { error = "Fel e-postadress eller lösenord." });

            SetRefreshCookie(result.RefreshToken);
            return Ok(new LoginResponse(result.AccessToken));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
                return Unauthorized(new { error = "Ingen giltig session." });

            var result = await _authService.RefreshAsync(refreshToken);
            if (result is null)
                return Unauthorized(new { error = "Sessionen har gått ut. Logga in igen." });

            SetRefreshCookie(result.RefreshToken);
            return Ok(new LoginResponse(result.AccessToken));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
            {
                await _authService.LogoutAsync(refreshToken);
            }

            Response.Cookies.Delete(RefreshCookieName);
            return Ok();
        }

        private void SetRefreshCookie(string refreshToken)
        {
            Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = false,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
            });
        }
    }
}