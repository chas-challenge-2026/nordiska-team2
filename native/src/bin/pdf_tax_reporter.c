#include "pdf_engine.h"

#include <stdio.h>
#include <stdlib.h>

// Manual smoke-test / demo tool: writes a single hardcoded mock tax report
// to a temporary JSON file (the engine takes a file path, not raw JSON
// text), feeds that file to pdf_engine_generate_and_sign(), then removes
// the temporary input. Only the generated PDF(s) under OUT_DIR are meant to
// stick around.

#define MOCK_JSON_PATH "mock_tax_data.json"
#define OUT_DIR "data"

static const char* const MOCK_TAX_REPORT_JSON =
    "[\n"
    "  {\n"
    "    \"metadata\": {\n"
    "      \"report_id\": \"TAX-2026-99001\",\n"
    "      \"report_type\": \"ANNUAL_TAX_REPORT\",\n"
    "      \"year\": 2026,\n"
    "      \"period_start\": \"2026-01-01\",\n"
    "      \"period_end\": \"2026-12-31\",\n"
    "      \"generation_date\": \"2027-01-02T02:00:00Z\"\n"
    "    },\n"
    "    \"customer\": {\n"
    "      \"customer_id\": \"1337\",\n"
    "      \"personal_id\": \"198505051234\",\n"
    "      \"full_name\": \"Anna Andersson\",\n"
    "      \"address\": \"Storgatan 1, 111 22 Stockholm\"\n"
    "    },\n"
    "    \"account\": {\n"
    "      \"account_number\": \"9150-123456789\",\n"
    "      \"account_type\": \"Sparkonto Plus\",\n"
    "      \"interest_rate_pct\": 3.50\n"
    "    },\n"
    "    \"summary\": {\n"
    "      \"starting_balance_sek\": 50000.00,\n"
    "      \"ending_balance_sek\": 55695.00,\n"
    "      \"total_deposits_sek\": 5000.00,\n"
    "      \"total_withdrawals_sek\": 600.00,\n"
    "      \"total_interest_earned_sek\": 1850.00,\n"
    "      \"total_tax_withheld_sek\": 555.00\n"
    "    },\n"
    "    \"transactions\": [\n"
    "      {\n"
    "        \"id\": \"TX-1001\",\n"
    "        \"date\": \"2026-02-15\",\n"
    "        \"type\": \"deposit\",\n"
    "        \"description\": \"Ins\xc3\xa4ttning\",\n"
    "        \"amount_sek\": 5000.00,\n"
    "        \"balance_after_sek\": 55000.00\n"
    "      },\n"
    "      {\n"
    "        \"id\": \"TX-1002\",\n"
    "        \"date\": \"2026-06-20\",\n"
    "        \"type\": \"withdrawal\",\n"
    "        \"description\": \"Uttag (Bank\xc3\xb6verf\xc3\xb6ring)\",\n"
    "        \"amount_sek\": -600.00,\n"
    "        \"balance_after_sek\": 54400.00\n"
    "      },\n"
    "      {\n"
    "        \"id\": \"TX-1003\",\n"
    "        \"date\": \"2026-12-31\",\n"
    "        \"type\": \"interest\",\n"
    "        \"description\": \"\xc3\x85rsr\xc3\xa4nta 2026 (3.50%)\",\n"
    "        \"amount_sek\": 1850.00,\n"
    "        \"balance_after_sek\": 56250.00\n"
    "      },\n"
    "      {\n"
    "        \"id\": \"TX-1004\",\n"
    "        \"date\": \"2026-12-31\",\n"
    "        \"type\": \"tax\",\n"
    "        \"description\": \"Prelimin\xc3\xa4rskatt (30% kapitalskatt)\",\n"
    "        \"amount_sek\": -555.00,\n"
    "        \"balance_after_sek\": 55695.00\n"
    "      }\n"
    "    ]\n"
    "  },\n"
    "  {\n"
    "    \"metadata\": {\n"
    "      \"report_id\": \"TAX-2026-99001\",\n"
    "      \"report_type\": \"ANNUAL_TAX_REPORT\",\n"
    "      \"year\": 2026,\n"
    "      \"period_start\": \"2026-01-01\",\n"
    "      \"period_end\": \"2026-12-31\",\n"
    "      \"generation_date\": \"2027-01-02T02:00:00Z\"\n"
    "    },\n"
    "    \"customer\": {\n"
    "      \"customer_id\": \"1337\",\n"
    "      \"personal_id\": \"198505051234\",\n"
    "      \"full_name\": \"Anna Andersson\",\n"
    "      \"address\": \"Storgatan 1, 111 22 Stockholm\"\n"
    "    },\n"
    "    \"account\": {\n"
    "      \"account_number\": \"9152-123452782\",\n"
    "      \"account_type\": \"Sparkonto Plus\",\n"
    "      \"interest_rate_pct\": 3.50\n"
    "    },\n"
    "    \"summary\": {\n"
    "      \"starting_balance_sek\": 50000.00,\n"
    "      \"ending_balance_sek\": 55695.00,\n"
    "      \"total_deposits_sek\": 5000.00,\n"
    "      \"total_withdrawals_sek\": 600.00,\n"
    "      \"total_interest_earned_sek\": 1850.00,\n"
    "      \"total_tax_withheld_sek\": 555.00\n"
    "    },\n"
    "    \"transactions\": [\n"
    "      {\n"
    "        \"id\": \"TX-1001\",\n"
    "        \"date\": \"2026-02-15\",\n"
    "        \"type\": \"deposit\",\n"
    "        \"description\": \"Ins\xc3\xa4ttning\",\n"
    "        \"amount_sek\": 5000.00,\n"
    "        \"balance_after_sek\": 55000.00\n"
    "      },\n"
    "      {\n"
    "        \"id\": \"TX-1002\",\n"
    "        \"date\": \"2026-06-20\",\n"
    "        \"type\": \"withdrawal\",\n"
    "        \"description\": \"Uttag (Bank\xc3\xb6verf\xc3\xb6ring)\",\n"
    "        \"amount_sek\": -600.00,\n"
    "        \"balance_after_sek\": 54400.00\n"
    "      },\n"
    "      {\n"
    "        \"id\": \"TX-1003\",\n"
    "        \"date\": \"2026-12-31\",\n"
    "        \"type\": \"interest\",\n"
    "        \"description\": \"\xc3\x85rsr\xc3\xa4nta 2026 (3.50%)\",\n"
    "        \"amount_sek\": 1850.00,\n"
    "        \"balance_after_sek\": 56250.00\n"
    "      },\n"
    "      {\n"
    "        \"id\": \"TX-1004\",\n"
    "        \"date\": \"2026-12-31\",\n"
    "        \"type\": \"tax\",\n"
    "        \"description\": \"Prelimin\xc3\xa4rskatt (30% kapitalskatt)\",\n"
    "        \"amount_sek\": -555.00,\n"
    "        \"balance_after_sek\": 55695.00\n"
    "      }\n"
    "    ]\n"
    "  }\n"
    "]";

static int write_mock_json_file(const char* path) {
    FILE* f = fopen(path, "wb");
    if (!f) {
        return -1;
    }
    fputs(MOCK_TAX_REPORT_JSON, f);
    fclose(f);
    return 0;
}

// Prints per-report progress; a stand-in for whatever the real
// caller wants to do with this (retry, log to a DB, etc).
static void print_progress(const PdfEngineReportResult* result,
                           void*                        user_ctx) {
    (void)user_ctx;
    if (result->status == PDF_ENGINE_SUCCESS) {
        printf("  OK    %s.pdf\n", result->report_id);
    } else {
        fprintf(stderr, "  FAIL  %s.pdf (status=%d)\n", result->report_id,
                result->status);
    }
}

int main(int argc, char** argv) {
    (void)argc;
    (void)argv;

    if (write_mock_json_file(MOCK_JSON_PATH) != 0) {
        fprintf(stderr, "Failed to write mock JSON fixture '%s'\n",
                MOCK_JSON_PATH);
        return 1;
    }

    printf("Generating mock tax report(s) into %s/...\n", OUT_DIR);
    int result =
        pdf_engine_generate_and_sign(MOCK_JSON_PATH, PDF_REPORT_TYPE_TAX_REPORT,
                                     OUT_DIR, NULL, NULL, print_progress, NULL);
    // input is temporary
    remove(MOCK_JSON_PATH);

    if (result < 0) {
        fprintf(stderr, "Failed to generate PDF, error code: %d\n", result);
        return 1;
    }

    printf("Generated %d report(s) in %s/\n", result, OUT_DIR);
    return (result > 0) ? 0 : 1;
}
