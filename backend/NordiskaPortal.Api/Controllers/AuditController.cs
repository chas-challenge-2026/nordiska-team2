using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.Extensions;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Controllers
{
    // The customer's own activity log ("Senaste aktivitet").
    //
    // The customer id comes from the token, never from the URL or query,
    // so there is no parameter anyone could change to read someone
    // else's history.
    // Same pattern as GET /api/accounts.
    //
    // Read-only by design: no POST, PUT or DELETE exists for audit
    // entries anywhere in the API.
    [ApiController]
    [Route("api/audit")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private const int MaxLimit = 100;

        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        // GET /api/audit?limit=20
        [HttpGet]
        public async Task<IActionResult> GetMyActivity([FromQuery] int limit = 50)
        {
            // Clamped rather than rejected: a caller asking for 10 000 rows gets 100, not an error. 
            // Stops one request from pulling the whole (ever-growing) table.
            limit = Math.Clamp(limit, 1, MaxLimit);

            var entries = await _auditService.GetForCustomerAsync(User.GetCustomerId(), limit);
            return Ok(entries);
        }
    }
}