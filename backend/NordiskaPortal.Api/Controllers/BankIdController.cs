using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Middleware;
using NordiskaPortal.Api.Services;

// BankID login: start -> poll status (with animated QR) -> complete.
// No [Authorize], since the caller isn't authenticated yet (this is the authentication).
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
        [SkipAuditCheck("Starts an in-memory order; the login itself is audited as bankid_login")]
        public async Task<IActionResult> Start(CancellationToken ct)
        {
            var result = await _bankIdService.StartAsync(GetEndUserIp(), ct);
            return Ok(new BankIdStartResponse(result.OrderRef, result.AutoStartToken));
        }

        [HttpGet("status/{orderRef}")]
        [EnableRateLimiting("BankIdPolling")]
        public async Task<IActionResult> GetStatus(string orderRef, CancellationToken ct)
        {
            var result = await _bankIdService.GetStatusAsync(orderRef, ct);

            if (result.Status == "complete" && result.RefreshToken != null)
            {
                SetRefreshCookie(result.RefreshToken);
                return Ok(new { status = result.Status, accessToken = result.AccessToken });
            }

            return Ok(new { status = result.Status, hintCode = result.HintCode, qrData = result.QrData });
        }

        [HttpPost("cancel/{orderRef}")]
        [SkipAuditCheck("Cancels an in-memory order; nothing is saved")]
        public async Task<IActionResult> Cancel(string orderRef, CancellationToken ct)
        {
            await _bankIdService.CancelAsync(orderRef, ct);
            return NoContent();
        }

        // BankID requires the end user's IP. Behind Docker/nginx this is the proxy's IP
        // until ForwardedHeaders is configured (Step 6).
        private string GetEndUserIp()
        {
            var ip = HttpContext.Connection.RemoteIpAddress;
            if (ip is null) return "127.0.0.1";
            return (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString();
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