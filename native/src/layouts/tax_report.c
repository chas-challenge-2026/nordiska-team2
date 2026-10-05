#include "tax_report.h"

#include "layouts/pdf_render.h"
#include "logging/log.h"
#include "utils/utils.h"

#include <stdio.h>

/* Everything this layout draws. The engine rejects a report that lacks any of
 * these before rendering, so the "-" / 0 fallbacks in the draw functions
 * below are unreachable for these fields. */
static const PdfRequiredField REQUIRED_FIELDS[] = {
    {"metadata", PDF_FIELD_OBJECT},
    {"metadata.report_id", PDF_FIELD_STRING},
    {"metadata.year", PDF_FIELD_NUMBER},
    {"metadata.period_start", PDF_FIELD_STRING},
    {"metadata.period_end", PDF_FIELD_STRING},

    {"customer", PDF_FIELD_OBJECT},
    {"customer.full_name", PDF_FIELD_STRING},
    {"customer.personal_id", PDF_FIELD_STRING},
    {"customer.address", PDF_FIELD_STRING},

    {"account", PDF_FIELD_OBJECT},
    {"account.account_number", PDF_FIELD_STRING},
    {"account.account_type", PDF_FIELD_STRING},
    {"account.interest_rate_pct", PDF_FIELD_NUMBER},

    {"summary", PDF_FIELD_OBJECT},
    {"summary.starting_balance_sek", PDF_FIELD_NUMBER},
    {"summary.ending_balance_sek", PDF_FIELD_NUMBER},
    {"summary.total_deposits_sek", PDF_FIELD_NUMBER},
    {"summary.total_withdrawals_sek", PDF_FIELD_NUMBER},
    {"summary.total_interest_earned_sek", PDF_FIELD_NUMBER},
    {"summary.total_tax_withheld_sek", PDF_FIELD_NUMBER},

    {"transactions", PDF_FIELD_ARRAY},
    {"transactions[].date", PDF_FIELD_STRING},
    {"transactions[].type", PDF_FIELD_STRING},
    {"transactions[].amount_sek", PDF_FIELD_NUMBER},
    {"transactions[].balance_after_sek", PDF_FIELD_NUMBER},
};

static int draw_title(RenderCtx* ctx, const cJSON* metadata) {
    char subtitle[192];
    snprintf(subtitle, sizeof(subtitle), "Report %s | Tax year %.0f | %s - %s",
             json_get_string(metadata, "report_id", "-"),
             json_get_number(metadata, "year", 0),
             json_get_string(metadata, "period_start", "-"),
             json_get_string(metadata, "period_end", "-"));
    return render_title_block(ctx, "Official Annual Tax Report", subtitle);
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
        {"Starting balance", ""}, {"Ending balance", ""},
        {"Total deposits", ""},   {"Total withdrawals", ""},
        {"Interest earned", ""},  {"Tax withheld", ""},
    };
    static const char* const KEYS[] = {
        "starting_balance_sek",      "ending_balance_sek",
        "total_deposits_sek",        "total_withdrawals_sek",
        "total_interest_earned_sek", "total_tax_withheld_sek"};
    for (size_t i = 0; i < 6; i++) {
        format_sek(json_get_number(summary, KEYS[i], 0.0), rows[i].value,
                   sizeof(rows[i].value));
    }
    int result = render_key_value_rows(ctx, rows, 6);
    ctx->y -= 10.0f;
    return result;
}

static int tax_report_layout(HPDF_Doc pdf, const cJSON* root) {
    if (!root) {
        LOG_ERROR("tax_report_layout called with no JSON data");
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
        LOG_ERROR("Failed to lay out tax report");
        return -4;
    }
    return 0;
}

// account_number is required above, so this always finds an id for reports
// that passed validation; the other candidates are kept as fallbacks.
static void tax_report_extract_id(const cJSON* root, char* out,
                                  size_t out_cap) {
    const cJSON* account  = cJSON_GetObjectItemCaseSensitive(root, "account");
    const cJSON* customer = cJSON_GetObjectItemCaseSensitive(root, "customer");
    const cJSON* metadata = cJSON_GetObjectItemCaseSensitive(root, "metadata");

    const cJSON* acc_num =
        cJSON_GetObjectItemCaseSensitive(account, "account_number");
    const cJSON* cust_id =
        cJSON_GetObjectItemCaseSensitive(customer, "customer_id");
    const cJSON* rep_id =
        cJSON_GetObjectItemCaseSensitive(metadata, "report_id");

    const char* candidate = NULL;
    if (cJSON_IsString(acc_num) && acc_num->valuestring[0]) {
        candidate = acc_num->valuestring;
    } else if (cJSON_IsString(cust_id) && cust_id->valuestring[0]) {
        candidate = cust_id->valuestring;
    } else if (cJSON_IsString(rep_id) && rep_id->valuestring[0]) {
        candidate = rep_id->valuestring;
    }
    if (candidate) {
        sanitize_filename_component(candidate, out, out_cap);
    }
}

const PdfLayoutConfig* tax_report_get_config(void) {
    static const PdfLayoutConfig CONFIG = {
        .layout_fn       = tax_report_layout,
        .extract_id_fn   = tax_report_extract_id,
        .required_fields = REQUIRED_FIELDS,
        .required_field_count =
            sizeof(REQUIRED_FIELDS) / sizeof(REQUIRED_FIELDS[0]),
    };
    return &CONFIG;
}
