//using System.Text.Json;
//using Microsoft.AspNetCore.Mvc;
//using NordiskaPortal.Api.Services;

//namespace NordiskaPortal.Api.Controllers;

//[ApiController]
//[Route("api/tax-reports")]
//public class TaxReportController : ControllerBase
//{
//    private readonly TaxReportService _taxReportService;
//    private readonly PdfGeneratorService _pdfGeneratorService;
//    private readonly IConfiguration _configuration;

//    public TaxReportController(TaxReportService taxReportService, PdfGeneratorService pdfGeneratorService, IConfiguration configuration)
//    {
//        _taxReportService = taxReportService;
//        _pdfGeneratorService = pdfGeneratorService;
//        _configuration = configuration;
//    }

//    // Namnge FETCH
//    [HttpGet("accounts/{accountId:int}/tax-report/{year:int}")]
//    public async Task<IActionResult> FetchTaxReport(int accountId, int year)
//    {
//        var report = await _taxReportService.BuildReportAsync(accountId, year);
//        if (report == null)
//        {
//            return NotFound(new { message = "Account not found" });
//        }

//        string outputDir = _configuration["PdfOutput:Directory"] ?? "pdf-output";

//        int generatedCount = _pdfGeneratorService.GenerateTaxReports(report, outputDir, null, null);

//        if (generatedCount <= 0)
//        {
//            return StatusCode(500, new { message = "PDF generation failed", code = generatedCount });
//        }

//        // Find the most recently created PDF in the output folder
//        var newestPdf = new DirectoryInfo(outputDir)
//            .GetFiles("*.pdf")
//            .OrderByDescending(f => f.CreationTimeUtc)
//            .FirstOrDefault();

//        if (newestPdf == null)
//            return StatusCode(500, new { message = "PDF reported as generated but not found on disk" });

//        var fileBytes = await System.IO.File.ReadAllBytesAsync(newestPdf.FullName);
//        return File(fileBytes, "application/pdf", newestPdf.Name);
//    }
//}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;
using NordiskaPortal.Api.Services;

namespace NordiskaPortal.Api.Controllers;

[ApiController]
[Route("api/tax-reports")]
public class TaxReportController : ControllerBase
{
    private readonly TaxReportService _taxReportService;
    private readonly PdfGeneratorService _pdfGeneratorService;
    private readonly BankContext _db;

    public TaxReportController(TaxReportService taxReportService, PdfGeneratorService pdfGeneratorService, BankContext db)
    {
        _taxReportService = taxReportService;
        _pdfGeneratorService = pdfGeneratorService;
        _db = db;
    }

    [HttpGet("{accountId:int}/available-years")]
    public async Task<IActionResult> GetAvailableYears(int accountId)
    {
        var years = await _db.TaxReports
            .Where(r => r.AccountId == accountId)
            .OrderByDescending(r => r.Year)
            .Select(r => r.Year)
            .ToListAsync();

        return Ok(years);
    }

    [HttpGet("{accountId:int}/{year:int}")]
    public async Task<IActionResult> GetReport(int accountId, int year)
    {
        if (year >= DateTime.UtcNow.Year)
            return BadRequest(new { message = "That year's report is not available yet." });

        var record = await _db.TaxReports
            .FirstOrDefaultAsync(r => r.AccountId == accountId && r.Year == year);

        if (record == null)
            return NotFound(new { message = "No report has been generated for this account and year." });

        return File(record.PdfData, "application/pdf", $"{record.ReportId}.pdf");
    }
}