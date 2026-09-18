using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Services;

// Mock BankID
// start/status polling shape only. 
// No [Authorize] on either action, since the caller isn't authenticated yet.
// (This is the authentication).
namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/auth/bankid")]
    public class BankIdController : ControllerBase
    {
        private const string RefreshCookieName = "refreshToken";
        private readonly IBankIdService _bankIdService;
        private readonly IConfiguration _config;

        public BankIdController(IBankIdService bankIdService, IConfiguration config)
        {
            _bankIdService = bankIdService;
            _config = config;
        }

        [HttpPost("start")]
        public IActionResult Start(BankIdStartRequest request)
        {
            var orderRef = _bankIdService.StartOrder(request.PersonalId);
            return Ok(new BankIdStartResponse(orderRef));
        }

        [HttpGet("status/{orderRef}")]
        public async Task<IActionResult> GetStatus(string orderRef)
        {
            var result = await _bankIdService.GetStatusAsync(orderRef);

            if (result.Status == "complete" && result.RefreshToken != null)
            {
                SetRefreshCookie(result.RefreshToken);
                return Ok(new { status = "complete", accessToken = result.AccessToken });
            }

            return Ok(new { status = result.Status });
        }

        private void SetRefreshCookie(string refreshToken)
        {
            var refreshTokenDays = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7);
            Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = false,
                Expires = DateTimeOffset.UtcNow.AddDays(refreshTokenDays),
            });
        }
    }
}