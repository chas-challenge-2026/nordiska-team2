using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("deposit")]
        [EnableRateLimiting("SensitiveEndpoints")]
        public async Task<IActionResult> Deposit(DepositRequest request)
        {
            // TODO (Epic 3): once JWT auth exists, verify the authenticated
            // customer actually owns this AccountId before allowing the deposit.
            var result = await _transactionService.DepositAsync(request.AccountId, request.Amount);
            return result.Success ? Ok(result.Entry) : BadRequest(new { error = result.Error });
        }

        [HttpPost("withdraw")]
        [EnableRateLimiting("SensitiveEndpoints")]
        public async Task<IActionResult> Withdraw(WithdrawRequest request)
        {
            // TODO (Epic 3): same ownership check as Deposit.
            var result = await _transactionService.WithdrawAsync(request.AccountId, request.Amount);
            return result.Success ? Ok(result.Entry) : BadRequest(new { error = result.Error });
        }

        [HttpGet("{accountId:int}")]
        public async Task<IActionResult> GetHistory(int accountId)
        {
            var history = await _transactionService.GetHistoryAsync(accountId);
            return Ok(history);
        }

        [HttpGet("{accountId:int}/balance")]
        public async Task<IActionResult> GetBalance(int accountId)
        {
            var balance = await _transactionService.GetBalanceAsync(accountId);
            return Ok(new { accountId, balance });
        }
    }
}