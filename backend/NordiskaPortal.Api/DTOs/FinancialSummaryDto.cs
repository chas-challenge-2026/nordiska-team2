namespace NordiskaPortal.Api.DTOs;

public record FinancialSummaryDto(
    decimal Income,
    decimal Expenses,
    DateTime PeriodStart,
    DateTime PeriodEnd
);
