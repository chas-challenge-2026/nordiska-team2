# Input format

The input is a single JSON file whose root is an **array of report objects**. Each object produces one PDF.

```json
[
  { "...": "report 1" },
  { "...": "report 2" }
]
```

Unknown keys are ignored. Fields are validated *before* rendering; a report that fails validation produces no PDF and is reported through the progress callback with `PDF_ENGINE_ERROR_MISSING_FIELD` and a detail string such as:

```
missing or wrong type: transactions[3].amount_sek (expected number)
```

Type meanings: **string** = non-empty string, **number** = any JSON number, **array** = may be empty, **object** = JSON object.

## Tax report (`PDF_REPORT_TYPE_TAX_REPORT`)

| Path | Type |
|---|---|
| `metadata.report_id` | string |
| `metadata.year` | number |
| `metadata.period_start` | string |
| `metadata.period_end` | string |
| `customer.full_name` | string |
| `customer.personal_id` | string |
| `customer.address` | string |
| `account.account_number` | string |
| `account.account_type` | string |
| `account.interest_rate_pct` | number |
| `summary.starting_balance_sek` | number |
| `summary.ending_balance_sek` | number |
| `summary.total_deposits_sek` | number |
| `summary.total_withdrawals_sek` | number |
| `summary.total_interest_earned_sek` | number |
| `summary.total_tax_withheld_sek` | number |
| `transactions` | array |
| `transactions[].date` | string |
| `transactions[].type` | string |
| `transactions[].amount_sek` | number |
| `transactions[].balance_after_sek` | number |

## Bank statement (`PDF_REPORT_TYPE_BANK_STATEMENT`)

| Path | Type | Notes |
|---|---|---|
| `metadata.statement_id` | string | |
| `metadata.period_start` | string | |
| `metadata.period_end` | string | |
| `customer.full_name` | string | |
| `customer.personal_id` | string | |
| `customer.address` | string | |
| `account.account_number` | string | |
| `account.account_type` | string | |
| `account.interest_rate_pct` | number | **Optional.** Rendered as `-` when absent |
| `summary.opening_balance_sek` | number | |
| `summary.closing_balance_sek` | number | |
| `summary.total_deposits_sek` | number | |
| `summary.total_withdrawals_sek` | number | |
| `summary.transaction_count` | number | |
| `transactions` | array | |
| `transactions[].date` | string | |
| `transactions[].type` | string | |
| `transactions[].amount_sek` | number | |
| `transactions[].balance_after_sek` | number | |

For both types, `transactions[].description` is optional and shows `-` when missing.

## Example (bank statement)

```json
[
  {
    "metadata": {
      "statement_id": "STMT-2026-09-0001",
      "period_start": "2026-09-01",
      "period_end": "2026-09-30"
    },
    "customer": {
      "full_name": "Anna Andersson",
      "personal_id": "19800101-1234",
      "address": "Storgatan 1, 111 22 Stockholm"
    },
    "account": {
      "account_number": "8327-9,123 456 789-0",
      "account_type": "Savings"
    },
    "summary": {
      "opening_balance_sek": 10000.00,
      "closing_balance_sek": 11500.00,
      "total_deposits_sek": 2000.00,
      "total_withdrawals_sek": 500.00,
      "transaction_count": 2
    },
    "transactions": [
      { "date": "2026-09-03", "type": "Deposit", "description": "Salary",
        "amount_sek": 2000.00, "balance_after_sek": 12000.00 },
      { "date": "2026-09-20", "type": "Withdrawal", "description": "Transfer",
        "amount_sek": -500.00, "balance_after_sek": 11500.00 }
    ]
  }
]
```

A full sample lives in [`examples/input/bank_statement.json`](../examples/input/bank_statement.json), and `scripts/generate_mock_json.py <count> <out.json>` generates large tax-report datasets.

## Output filenames

Each PDF is written to `<out_dir>/<id>.pdf`. The id is sanitized to safe filename characters, so it may differ slightly from the source value.

| Report type | Id source (first non-empty wins) |
|---|---|
| Tax report | `account.account_number`, `customer.customer_id`, `metadata.report_id` |
| Bank statement | `metadata.statement_id`, `account.account_number` |

Bank statements prefer `statement_id` because one account has one statement per period; using the account number would make files overwrite each other. If two reports in the same batch resolve to the same id, the later one replaces the earlier.

## Rendering limits

- Fonts: built-in Helvetica / Helvetica-Bold, WinAnsi (CP1252). Latin-1 characters render; anything else becomes `?`.
- Individual text strings are limited to 255 bytes; longer values are truncated.
- Page size is A4 portrait; transactions paginate automatically with a repeated table header.
- Amounts are formatted as `1234.56 SEK`.
