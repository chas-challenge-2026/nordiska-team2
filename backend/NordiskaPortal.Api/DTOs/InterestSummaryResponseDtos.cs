namespace NordiskaPortal.Api.DTOs;

public record InterestSummaryResponse(
    int Year,
    decimal InterestRatePct,
    decimal EarnedToDate,
    decimal ProjectedAtYearEnd,
    decimal InterestPaidTotal,
    DateOnly PaymentDate);
