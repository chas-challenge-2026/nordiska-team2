using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.Middleware;
using NordiskaPortal.Api.Services;

// DEV ONLY: simulates the user approving on their phone.
// Answers 404 unless running in Development with the mock active, so it can't be used against real BankID.
namespace NordiskaPortal.Api.Controllers
{
    public record BankIdMockApproveRequest(string PersonalId);

    [ApiController]
    [Route("api/auth/bankid/mock")]
    public class BankIdMockController : ControllerBase
    {
        private readonly IBankIdService _bankIdService;
        private readonly IWebHostEnvironment _env;

        public BankIdMockController(IBankIdService bankIdService, IWebHostEnvironment env)
        {
            _bankIdService = bankIdService;
            _env = env;
        }

        [HttpPost("approve/{orderRef}")]
        [SkipAuditCheck("Dev-only mock: simulates the phone, changes no data")]
        public IActionResult Approve(string orderRef, BankIdMockApproveRequest request)
        {
            if (!_env.IsDevelopment() || _bankIdService is not MockBankIdService mock)
                return NotFound();

            return mock.Approve(orderRef, request.PersonalId) ? NoContent() : NotFound();
        }
    }
}