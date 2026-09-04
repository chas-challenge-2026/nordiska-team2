using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Controllers;

[ApiController]
[Route("api/tax-reports")]
public class TaxReportController : ControllerBase
{
    private readonly TaxReportService _taxReportService;
    private readonly PdfGeneratorService _pdfGeneratorService;
    private readonly IConfiguration _configuration;

    public TaxReportController(TaxReportService taxReportService, PdfGeneratorService pdfGeneratorService, IConfiguration configuration)
    {
        _taxReportService = taxReportService;
        _pdfGeneratorService = pdfGeneratorService;
        _configuration = configuration;
    }

    [HttpGet("{accountId:int}/{year:int}/test")]
    public IActionResult TestGenerateReport(int accountId, int year)
    {
        string outputDir = _configuration["PdfOutput:Directory"] ?? "pdf-output";
        Directory.CreateDirectory(outputDir);

        var metadata = _taxReportService.BuildMetadata(accountId, year);
        string json = System.Text.Json.JsonSerializer.Serialize(metadata);

        string fileName = $"{metadata.ReportId}.pdf";
        string outPath = Path.Combine(outputDir, fileName);

        bool success = _pdfGeneratorService.GenerateTaxReport(json, outPath, null, null);

        return success
            ? Ok(new { message = "PDF generated", path = outPath, fileName })
            : StatusCode(500, new { message = "PDF generation failed" });
    }

    [HttpGet("{jobId:guid}")]
    public IActionResult GetReportStatus(Guid jobId)
    {
        throw new NotImplementedException();
    }
}