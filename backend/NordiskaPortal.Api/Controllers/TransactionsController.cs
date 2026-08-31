using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    [HttpPost("deposit")]
    [EnableRateLimiting("SensitiveEndpoints")]
    public IActionResult Deposit(DepositRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpPost("withdraw")]
    [EnableRateLimiting("SensitiveEndpoints")]
    public IActionResult Withdraw(WithdrawRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{accountId:guid}")]
    public IActionResult GetHistory(Guid accountId)
    {
        throw new NotImplementedException();
    }
}