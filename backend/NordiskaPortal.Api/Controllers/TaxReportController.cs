using Microsoft.AspNetCore.Mvc;

namespace NordiskaPortal.Api.Controllers;

[ApiController]
[Route("api/tax-reports")]
public class TaxReportController : ControllerBase
{
    [HttpPost]
    public IActionResult RequestReport(Guid accountId)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{jobId:guid}")]
    public IActionResult GetReportStatus(Guid jobId)
    {
        throw new NotImplementedException();
    }
}