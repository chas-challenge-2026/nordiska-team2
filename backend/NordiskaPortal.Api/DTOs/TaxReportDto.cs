using System;

namespace NordiskaPortal.Api.DTOs
{
    public record TaxReportMetadataDto(
        string ReportId,
        string ReportType,
        int Year,
        DateTime PeriodStart,
        DateTime PeriodEnd,
        DateTime GenerationDate
    );
}