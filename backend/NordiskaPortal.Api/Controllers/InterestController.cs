using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.DTOs;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Controllers;

[ApiController]
[Route("api/accounts/{accountId:int}/interest")]
public class InterestController : ControllerBase
{
    private readonly InterestService _interestService;

    public InterestController(InterestService interestService)
    {
        _interestService = interestService;
    }

    [HttpGet]
    public async Task<ActionResult<InterestSummaryResponse>> GetInterestSummary(int accountId)
    {
        var summary = await _interestService.GetInterestSummaryAsync(accountId);

        return Ok(summary);
    }
}
