using System;
using System.Text.Json.Serialization;

namespace NordiskaPortal.Api.DTOs
{
    public record TaxReportDto(
        [property: JsonPropertyName("metadata")]
        TaxReportMetadataDto Metadata,

        [property: JsonPropertyName("customer")]
        TaxReportCustomerDto Customer,

        [property: JsonPropertyName("account")]
        TaxReportAccountDto Account,

        [property: JsonPropertyName("summary")]
        TaxReportSummaryDto Summary,

        [property: JsonPropertyName("transactions")]
        List<TaxReportTransactionDto> Transactions
    );

    public record TaxReportMetadataDto(
        [property: JsonPropertyName("report_id")]
        string ReportId,

        [property: JsonPropertyName("report_type")]
        string ReportType,

        [property: JsonPropertyName("year")]
        int Year,

        [property: JsonPropertyName("period_start")]
        string PeriodStart,

        [property: JsonPropertyName("period_end")]
        string PeriodEnd,

        [property: JsonPropertyName("generation_date")]
        string GenerationDate
    );

    public record TaxReportCustomerDto(
        [property: JsonPropertyName("customer_id")]
        string CustomerId,

        [property: JsonPropertyName("personal_id")]
        string PersonalId,

        [property: JsonPropertyName("full_name")]
        string FullName,

        [property: JsonPropertyName("address")]
        string Address
    );

    public record TaxReportAccountDto(
        [property: JsonPropertyName("account_number")]
        string AccountNumber,

        [property: JsonPropertyName("account_type")]
        string AccountType,

        [property: JsonPropertyName("interest_rate_pct")]
        decimal InterestRatePct
    );

    public record TaxReportSummaryDto(
        [property: JsonPropertyName("starting_balance_sek")]
        decimal StartingBalanceSek,

        [property: JsonPropertyName("ending_balance_sek")]
        decimal EndingBalanceSek,

        [property: JsonPropertyName("total_deposits_sek")]
        decimal TotalDepositsSek,

        [property: JsonPropertyName("total_withdrawals_sek")]
        decimal TotalWithdrawalsSek,

        [property: JsonPropertyName("total_interest_earned_sek")]
        decimal TotalInterestEarnedSek,

        [property: JsonPropertyName("total_tax_withheld_sek")]
        decimal TotalTaxWithheldSek
    );

    public record TaxReportTransactionDto(
        [property: JsonPropertyName("id")]
        string Id,

        [property: JsonPropertyName("date")]
        string Date,

        [property: JsonPropertyName("type")]
        string Type,

        [property: JsonPropertyName("description")]
        string Description,

        [property: JsonPropertyName("amount_sek")]
        decimal AmountSek,

        [property: JsonPropertyName("balance_after_sek")]
        decimal BalanceAfterSek
    );
}