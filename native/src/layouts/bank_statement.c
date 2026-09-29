#include "bank_statement.h"

#include "layouts/pdf_render.h"
#include "logging/log.h"
#include "utils/utils.h"

#include <stdio.h>

static int draw_title(RenderCtx* ctx, const cJSON* metadata) {
    char subtitle[192];
    snprintf(subtitle, sizeof(subtitle), "Statement %s | Period %s - %s",
             json_get_string(metadata, "statement_id", "-"),
             json_get_string(metadata, "period_start", "-"),
             json_get_string(metadata, "period_end", "-"));
    return render_title_block(ctx, "Account Statement", subtitle);
}

static int draw_customer_section(RenderCtx* ctx, const cJSON* customer) {
    if (render_section_title(ctx, "Customer") != 0) {
        return -1;
    }
    KeyValueRow rows[] = {{"Name", ""}, {"Personal ID", ""}, {"Address", ""}};
    snprintf(rows[0].value, sizeof(rows[0].value), "%s",
             json_get_string(customer, "full_name", "-"));
    snprintf(rows[1].value, sizeof(rows[1].value), "%s",
             json_get_string(customer, "personal_id", "-"));
    snprintf(rows[2].value, sizeof(rows[2].value), "%s",
             json_get_string(customer, "address", "-"));
    int result = render_key_value_rows(ctx, rows, 3);
    ctx->y -= 10.0f;
    return result;
}

static int draw_account_section(RenderCtx* ctx, const cJSON* account) {
    if (render_section_title(ctx, "Account") != 0) {
        return -1;
    }
    KeyValueRow rows[] = {
        {"Account number", ""}, {"Account type", ""}, {"Interest rate", ""}};
    snprintf(rows[0].value, sizeof(rows[0].value), "%s",
             json_get_string(account, "account_number", "-"));
    snprintf(rows[1].value, sizeof(rows[1].value), "%s",
             json_get_string(account, "account_type", "-"));
    snprintf(rows[2].value, sizeof(rows[2].value), "%.2f%%",
             json_get_number(account, "interest_rate_pct", 0.0));
    int result = render_key_value_rows(ctx, rows, 3);
    ctx->y -= 10.0f;
    return result;
}

static int draw_summary_section(RenderCtx* ctx, const cJSON* summary) {
    if (render_section_title(ctx, "Summary") != 0) {
        return -1;
    }
    KeyValueRow rows[] = {
        {"Opening balance", ""}, {"Closing balance", ""},
        {"Total deposits", ""},  {"Total withdrawals", ""},
        {"Transactions", ""},
    };
    static const char* const KEYS[] = {
        "opening_balance_sek", "closing_balance_sek", "total_deposits_sek",
        "total_withdrawals_sek"};
    for (size_t i = 0; i < 4; i++) {
        format_sek(json_get_number(summary, KEYS[i], 0.0), rows[i].value,
                   sizeof(rows[i].value));
    }
    snprintf(rows[4].value, sizeof(rows[4].value), "%.0f",
             json_get_number(summary, "transaction_count", 0.0));
    int result = render_key_value_rows(ctx, rows, 5);
    ctx->y -= 10.0f;
    return result;
}

static int bank_statement_layout(HPDF_Doc pdf, const cJSON* root) {
    if (!root) {
        LOG_ERROR("bank_statement_layout called with no JSON data");
        return -1;
    }
    RenderCtx ctx;
    int       rc = render_ctx_init(&ctx, pdf);
    if (rc != 0) {
        return rc;
    }

    const cJSON* metadata = cJSON_GetObjectItemCaseSensitive(root, "metadata");
    const cJSON* customer = cJSON_GetObjectItemCaseSensitive(root, "customer");
    const cJSON* account  = cJSON_GetObjectItemCaseSensitive(root, "account");
    const cJSON* summary  = cJSON_GetObjectItemCaseSensitive(root, "summary");
    const cJSON* transactions =
        cJSON_GetObjectItemCaseSensitive(root, "transactions");

    if (draw_title(&ctx, metadata) != 0 ||
        draw_customer_section(&ctx, customer) != 0 ||
        draw_account_section(&ctx, account) != 0 ||
        draw_summary_section(&ctx, summary) != 0 ||
        render_transactions_section(&ctx, transactions) != 0) {
        LOG_ERROR("Failed to lay out bank statement");
        return -4;
    }
    return 0;
}

// statement_id first: the same account has one statement per period, so
// account_number alone would make files overwrite each other.
static void bank_statement_extract_id(const cJSON* root, char* out,
                                      size_t out_cap) {
    const cJSON* metadata = cJSON_GetObjectItemCaseSensitive(root, "metadata");
    const cJSON* account  = cJSON_GetObjectItemCaseSensitive(root, "account");

    const cJSON* stmt_id =
        cJSON_GetObjectItemCaseSensitive(metadata, "statement_id");
    const cJSON* acc_num =
        cJSON_GetObjectItemCaseSensitive(account, "account_number");

    const char* candidate = NULL;
    if (cJSON_IsString(stmt_id) && stmt_id->valuestring[0]) {
        candidate = stmt_id->valuestring;
    } else if (cJSON_IsString(acc_num) && acc_num->valuestring[0]) {
        candidate = acc_num->valuestring;
    }
    if (candidate) {
        sanitize_filename_component(candidate, out, out_cap);
    }
}

const PdfLayoutConfig* bank_statement_get_config(void) {
    static const PdfLayoutConfig CONFIG = {.layout_fn = bank_statement_layout,
                                           .extract_id_fn =
                                               bank_statement_extract_id};
    return &CONFIG;
}
