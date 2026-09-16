using Microsoft.EntityFrameworkCore;
using NordiskaPortal.Api.Data;
using NordiskaPortal.Api.Models;

namespace NordiskaPortal.Api.Services
{
    public class TaxReportBackfillService
    {
        private readonly BankContext _db;
        private readonly TaxReportService _taxReportService;
        private readonly PdfGeneratorService _pdfGeneratorService;
        private readonly ILogger<TaxReportBackfillService> _logger;

        private static readonly (int AccountId, int Year)[] BackfillTargets =
        {
            (1, 2023),
            (1, 2024),
            (1, 2025),
        };

        public TaxReportBackfillService(
            BankContext db,
            TaxReportService taxReportService,
            PdfGeneratorService pdfGeneratorService,
            ILogger<TaxReportBackfillService> logger)
        {
            _db = db;
            _taxReportService = taxReportService;
            _pdfGeneratorService = pdfGeneratorService;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            foreach (var (accountId, year) in BackfillTargets)
            {
                bool reportExists = await _db.TaxReports.AnyAsync(r => r.AccountId == accountId && r.Year == year);
                if (reportExists)
                {
                    _logger.LogInformation("Report already exists for account {AccountId}, year {Year} - skipping.", accountId, year);
                    continue;
                }

                await _taxReportService.ApplyYearEndInterestAsync(accountId, year);

                var reportData = await _taxReportService.BuildReportAsync(accountId, year);
                if (reportData == null)
                {
                    _logger.LogWarning("BuildReportAsync returned null for account {AccountId}, year {Year} - account not found?", accountId, year);
                    continue;
                }

                byte[]? pdfBytes = _pdfGeneratorService.GenerateSingleReportPdf(reportData, null, null);
                if (pdfBytes == null)
                {
                    _logger.LogWarning("PDF generation FAILED for account {AccountId}, year {Year}.", accountId, year);
                    continue;
                }

                _db.TaxReports.Add(new TaxReport
                {
                    AccountId = accountId,
                    Year = year,
                    ReportId = reportData.Metadata.ReportId,
                    PdfData = pdfBytes,
                    GeneratedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                _logger.LogInformation("Generated and stored report for account {AccountId}, year {Year}.", accountId, year);
            }
        }
    }
}