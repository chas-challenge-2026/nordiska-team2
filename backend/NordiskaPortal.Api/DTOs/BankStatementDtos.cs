using System.Text.Json.Serialization;

namespace NordiskaPortal.Api.DTOs
{
    public record BankStatementDto(
        [property: JsonPropertyName("metadata")]
        BankStatementMetadataDto Metadata,

        [property: JsonPropertyName("customer")]
        BankStatementCustomerDto Customer,

        [property: JsonPropertyName("account")]
        BankStatementAccountDto Account,

        [property: JsonPropertyName("summary")]
        BankStatementSummaryDto Summary,

        [property: JsonPropertyName("transactions")]
        List<BankStatementTransactionDto> Transactions
    );

    public record BankStatementMetadataDto(
        [property: JsonPropertyName("statement_id")]
        string StatementId,

        [property: JsonPropertyName("period_start")]
        string PeriodStart,

        [property: JsonPropertyName("period_end")]
        string PeriodEnd
    );

    public record BankStatementCustomerDto(
        [property: JsonPropertyName("full_name")]
        string FullName,

        [property: JsonPropertyName("personal_id")]
        string PersonalId,

        [property: JsonPropertyName("address")]
        string Address
    );

    public record BankStatementAccountDto(
        [property: JsonPropertyName("account_number")]
        string AccountNumber,

        [property: JsonPropertyName("account_type")]
        string AccountType,

        // Optional per the C layout: current accounts have none, and the
        // engine renders "-" when this key is entirely absent from the
        // JSON. System.Text.Json omits a null record property by default,
        // so leaving this null reproduces that "absent" behavior correctly
        // rather than sending an explicit 0.
        [property: JsonPropertyName("interest_rate_pct")]
        decimal? InterestRatePct
    );

    public record BankStatementSummaryDto(
        [property: JsonPropertyName("opening_balance_sek")]
        decimal OpeningBalanceSek,

        [property: JsonPropertyName("closing_balance_sek")]
        decimal ClosingBalanceSek,

        [property: JsonPropertyName("total_deposits_sek")]
        decimal TotalDepositsSek,

        [property: JsonPropertyName("total_withdrawals_sek")]
        decimal TotalWithdrawalsSek,

        [property: JsonPropertyName("transaction_count")]
        int TransactionCount
    );

    public record BankStatementTransactionDto(
        [property: JsonPropertyName("date")]
        string Date,

        [property: JsonPropertyName("type")]
        string Type,

        [property: JsonPropertyName("amount_sek")]
        decimal AmountSek,

        [property: JsonPropertyName("balance_after_sek")]
        decimal BalanceAfterSek
    );
}
