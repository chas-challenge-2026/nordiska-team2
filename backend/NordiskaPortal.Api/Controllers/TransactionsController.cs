using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Extensions;
using NordiskaPortal.Api.Services;

// All actions require [Authorize] and verify the authenticated customer
// actually owns the target account before doing anything with it.

// Deliberately returns the same "not found" message whether the account
// genuinely doesn't exist or just isn't the caller's.
// (Attackers probing account IDs can't distinguish between the two)

namespace NordiskaPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IAccountService _accountService;

        public TransactionsController(ITransactionService transactionService, IAccountService accountService)
        {
            _transactionService = transactionService;
            _accountService = accountService;
        }

        [HttpPost("deposit")]
        [EnableRateLimiting("SensitiveEndpoints")]
        public async Task<IActionResult> Deposit(DepositRequest request)
        {
            var customerId = User.GetCustomerId();
            if (!await _accountService.CustomerOwnsAccountAsync(customerId, request.AccountId))
                return NotFound(new { error = "Kontot kunde inte hittas." });

            var result = await _transactionService.DepositAsync(request.AccountId, request.Amount);
            return result.Success ? Ok(result.Entry) : BadRequest(new { error = result.Error });
        }

        [HttpPost("withdraw")]
        [EnableRateLimiting("SensitiveEndpoints")]
        public async Task<IActionResult> Withdraw(WithdrawRequest request)
        {
            var customerId = User.GetCustomerId();
            if (!await _accountService.CustomerOwnsAccountAsync(customerId, request.AccountId))
                return NotFound(new { error = "Kontot kunde inte hittas." });

            var result = await _transactionService.WithdrawAsync(request.AccountId, request.Amount);
            return result.Success ? Ok(result.Entry) : BadRequest(new { error = result.Error });
        }

        [HttpGet("{accountId:int}")]
        public async Task<IActionResult> GetHistory(int accountId)
        {
            var customerId = User.GetCustomerId();
            if (!await _accountService.CustomerOwnsAccountAsync(customerId, accountId))
                return NotFound(new { error = "Kontot kunde inte hittas." });

            var history = await _transactionService.GetHistoryAsync(accountId);
            return Ok(history);
        }

        [HttpGet("{accountId:int}/balance")]
        public async Task<IActionResult> GetBalance(int accountId)
        {
            var customerId = User.GetCustomerId();
            if (!await _accountService.CustomerOwnsAccountAsync(customerId, accountId))
                return NotFound(new { error = "Kontot kunde inte hittas." });

            var balance = await _transactionService.GetBalanceAsync(accountId);
            return Ok(new { accountId, balance });
        }
    }
}