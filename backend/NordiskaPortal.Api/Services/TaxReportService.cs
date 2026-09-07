using NordiskaPortal.Api.DTOs;

namespace NordiskaPortal.Api.Services
{
    public class TaxReportService
    {
        public TaxReportMetadataDto BuildMetadata(int accountId, int year)
        {
            string reportId = $"TAX-{year}-{accountId:D5}";

            return new TaxReportMetadataDto(
                ReportId: reportId,
                ReportType: "ANNUAL_TAX_REPORT",
                Year: year,
                PeriodStart: new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PeriodEnd: new DateTime(year, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                GenerationDate: DateTime.UtcNow
            );
        }
    }
}
