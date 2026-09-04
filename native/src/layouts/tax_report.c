#include "tax_report.h"

#include "logging/log.h"

#include <stdio.h>
#include <string.h>

// A4 in points. HPDF_Page_SetSize() sets the actual page geometry; these
// constants mirror that so the layout math below doesn't have to query it.
#define PAGE_WIDTH 595.0f
#define PAGE_HEIGHT 842.0f

#define MARGIN_LEFT 50.0f
#define MARGIN_RIGHT 50.0f
#define MARGIN_TOP 50.0f
#define MARGIN_BOTTOM 50.0f

#define CONTENT_TOP (PAGE_HEIGHT - MARGIN_TOP)
#define CONTENT_BOTTOM MARGIN_BOTTOM
#define CONTENT_RIGHT (PAGE_WIDTH - MARGIN_RIGHT)

#define FONT_SIZE_TITLE 18.0f
#define FONT_SIZE_SUBTITLE 10.0f
#define FONT_SIZE_SECTION 12.0f
#define FONT_SIZE_BODY 9.5f

#define LINE_HEIGHT_BODY 15.0f
#define LINE_HEIGHT_SECTION_GAP 22.0f
#define TABLE_ROW_HEIGHT 16.0f

// Transaction table column x-positions.
#define COL_DATE_X MARGIN_LEFT
#define COL_TYPE_X (MARGIN_LEFT + 65.0f)
#define COL_DESC_X (MARGIN_LEFT + 135.0f)
#define COL_AMOUNT_RIGHT_X (MARGIN_LEFT + 380.0f)
#define COL_BALANCE_RIGHT_X CONTENT_RIGHT

/** @brief A single row for the generic label/value section renderer. */
typedef struct {
    const char* label;
    char        value[160];
} KeyValueRow;

/** @brief Rendering cursor and resources shared by every draw_* helper. */
typedef struct {
    HPDF_Doc  pdf;
    HPDF_Page page;
    HPDF_Font font_regular;
    HPDF_Font font_bold;
    float     y;
} RenderCtx;

/** @brief Returns obj[key] as a string, or `fallback` if missing/wrong type. */
static const char* json_get_string(const cJSON* obj, const char* key,
                                   const char* fallback) {
    const cJSON* item = cJSON_GetObjectItemCaseSensitive(obj, key);
    if (cJSON_IsString(item) && item->valuestring) {
        return item->valuestring;
    }
    return fallback;
}

/** @brief Returns obj[key] as a double, or `fallback` if missing/wrong type. */
static double json_get_number(const cJSON* obj, const char* key,
                              double fallback) {
    const cJSON* item = cJSON_GetObjectItemCaseSensitive(obj, key);
    if (cJSON_IsNumber(item)) {
        return item->valuedouble;
    }
    return fallback;
}

static void format_sek(double amount, char* buf, size_t buf_len) {
    snprintf(buf, buf_len, "%.2f SEK", amount);
}

/**
 * @brief Converts a UTF-8 string to single-byte WinAnsiEncoding (CP1252) in
 * place, dropping/questioning-marking anything outside the Latin-1 range.
 *
 * libHaru's base-14 fonts draw raw bytes against whatever encoding they were
 * loaded with; they don't understand multi-byte UTF-8. Report text (names,
 * addresses, Swedish transaction descriptions) comes out of cJSON as UTF-8,
 * so it needs this conversion before HPDF_Page_TextOut(). Latin-1 Supplement
 * codepoints (U+0080-U+00FF), which cover å/ä/ö and friends, map 1:1 onto
 * WinAnsiEncoding byte values.
 */
static void utf8_to_winansi(const char* utf8, char* out, size_t out_cap) {
    size_t out_len = 0;
    for (const unsigned char* p = (const unsigned char*)utf8;
         *p != '\0' && out_len + 1 < out_cap; p++) {
        if (*p < 0x80) {
            out[out_len++] = (char)*p;
        } else if ((*p & 0xE0) == 0xC0 && (p[1] & 0xC0) == 0x80) {
            unsigned int codepoint =
                ((unsigned int)(*p & 0x1F) << 6) | (p[1] & 0x3F);
            if (codepoint <= 0xFF) {
                // memcpy avoids an implementation-defined narrowing cast
                // from an out-of-range int into a signed char.
                unsigned char byte = (unsigned char)codepoint;
                memcpy(&out[out_len], &byte, 1);
            } else {
                out[out_len] = '?';
            }
            out_len++;
            p++;
        } else {
            out[out_len++] = '?';
        }
    }
    out[out_len] = '\0';
}

/** @brief Starts a fresh A4 page and resets the write cursor to the top. */
static int start_new_page(RenderCtx* ctx) {
    HPDF_Page page = HPDF_AddPage(ctx->pdf);
    if (!page) {
        return -1;
    }
    HPDF_Page_SetSize(page, HPDF_PAGE_SIZE_A4, HPDF_PAGE_PORTRAIT);
    ctx->page = page;
    ctx->y    = CONTENT_TOP;
    return 0;
}

/**
 * @brief Guarantees at least `needed_height` of space above the bottom
 * margin, starting a new page first if the current one has run out.
 */
static int ensure_space(RenderCtx* ctx, float needed_height) {
    if (ctx->y - needed_height >= CONTENT_BOTTOM) {
        return 0;
    }
    return start_new_page(ctx);
}

static void draw_text_left(RenderCtx* ctx, HPDF_Font font, float size, float x,
                           const char* text) {
    char encoded[256];
    utf8_to_winansi(text, encoded, sizeof(encoded));

    HPDF_Page_SetFontAndSize(ctx->page, font, size);
    HPDF_Page_BeginText(ctx->page);
    HPDF_Page_TextOut(ctx->page, x, ctx->y, encoded);
    HPDF_Page_EndText(ctx->page);
}

static void draw_text_right(RenderCtx* ctx, HPDF_Font font, float size,
                            float right_x, const char* text) {
    char encoded[256];
    utf8_to_winansi(text, encoded, sizeof(encoded));

    HPDF_Page_SetFontAndSize(ctx->page, font, size);
    float width = (float)HPDF_Page_TextWidth(ctx->page, encoded);
    HPDF_Page_BeginText(ctx->page);
    HPDF_Page_TextOut(ctx->page, right_x - width, ctx->y, encoded);
    HPDF_Page_EndText(ctx->page);
}

static void draw_rule(RenderCtx* ctx) {
    HPDF_Page_SetLineWidth(ctx->page, 0.5f);
    HPDF_Page_MoveTo(ctx->page, MARGIN_LEFT, ctx->y);
    HPDF_Page_LineTo(ctx->page, CONTENT_RIGHT, ctx->y);
    HPDF_Page_Stroke(ctx->page);
}

/** @brief Draws a bold section heading followed by a rule, e.g. "Customer". */
static int draw_section_title(RenderCtx* ctx, const char* title) {
    if (ensure_space(ctx, LINE_HEIGHT_SECTION_GAP + LINE_HEIGHT_BODY) != 0) {
        return -1;
    }
    draw_text_left(ctx, ctx->font_bold, FONT_SIZE_SECTION, MARGIN_LEFT, title);
    ctx->y -= 4.0f;
    draw_rule(ctx);
    ctx->y -= LINE_HEIGHT_BODY;
    return 0;
}

/**
 * @brief Renders a two-column "label: value" block, one row per array entry.
 *
 * This is the generic building block behind the Customer/Account/Summary
 * sections: adding, removing, or reordering a field in the report is a
 * one-line change to the KeyValueRow array passed in by the caller.
 */
static int draw_key_value_rows(RenderCtx* ctx, const KeyValueRow* rows,
                               size_t row_count) {
    const float LABEL_X = MARGIN_LEFT;
    const float VALUE_X = MARGIN_LEFT + 150.0f;

    for (size_t i = 0; i < row_count; i++) {
        if (ensure_space(ctx, LINE_HEIGHT_BODY) != 0) {
            return -1;
        }
        char label_with_colon[64];
        snprintf(label_with_colon, sizeof(label_with_colon),
                 "%s:", rows[i].label);
        draw_text_left(ctx, ctx->font_bold, FONT_SIZE_BODY, LABEL_X,
                       label_with_colon);
        draw_text_left(ctx, ctx->font_regular, FONT_SIZE_BODY, VALUE_X,
                       rows[i].value);
        ctx->y -= LINE_HEIGHT_BODY;
    }
    return 0;
}

static int draw_title_block(RenderCtx* ctx, const cJSON* metadata) {
    if (ensure_space(ctx, 60.0f) != 0) {
        return -1;
    }
    draw_text_left(ctx, ctx->font_bold, FONT_SIZE_TITLE, MARGIN_LEFT,
                   "Official Annual Tax Report");
    ctx->y -= 20.0f;

    char subtitle[192];
    snprintf(subtitle, sizeof(subtitle), "Report %s | Tax year %.0f | %s - %s",
             json_get_string(metadata, "report_id", "-"),
             json_get_number(metadata, "year", 0),
             json_get_string(metadata, "period_start", "-"),
             json_get_string(metadata, "period_end", "-"));
    draw_text_left(ctx, ctx->font_regular, FONT_SIZE_SUBTITLE, MARGIN_LEFT,
                   subtitle);
    ctx->y -= LINE_HEIGHT_SECTION_GAP;
    return 0;
}

static int draw_customer_section(RenderCtx* ctx, const cJSON* customer) {
    if (draw_section_title(ctx, "Customer") != 0) {
        return -1;
    }
    KeyValueRow rows[] = {
        {"Name", ""},
        {"Personal ID", ""},
        {"Address", ""},
    };
    snprintf(rows[0].value, sizeof(rows[0].value), "%s",
             json_get_string(customer, "full_name", "-"));
    snprintf(rows[1].value, sizeof(rows[1].value), "%s",
             json_get_string(customer, "personal_id", "-"));
    snprintf(rows[2].value, sizeof(rows[2].value), "%s",
             json_get_string(customer, "address", "-"));

    int result = draw_key_value_rows(ctx, rows, sizeof(rows) / sizeof(rows[0]));
    ctx->y -= 10.0f;
    return result;
}

static int draw_account_section(RenderCtx* ctx, const cJSON* account) {
    if (draw_section_title(ctx, "Account") != 0) {
        return -1;
    }
    KeyValueRow rows[] = {
        {"Account number", ""},
        {"Account type", ""},
        {"Interest rate", ""},
    };
    snprintf(rows[0].value, sizeof(rows[0].value), "%s",
             json_get_string(account, "account_number", "-"));
    snprintf(rows[1].value, sizeof(rows[1].value), "%s",
             json_get_string(account, "account_type", "-"));
    snprintf(rows[2].value, sizeof(rows[2].value), "%.2f%%",
             json_get_number(account, "interest_rate_pct", 0.0));

    int result = draw_key_value_rows(ctx, rows, sizeof(rows) / sizeof(rows[0]));
    ctx->y -= 10.0f;
    return result;
}

static int draw_summary_section(RenderCtx* ctx, const cJSON* summary) {
    if (draw_section_title(ctx, "Summary") != 0) {
        return -1;
    }
    KeyValueRow rows[] = {
        {"Starting balance", ""}, {"Ending balance", ""},
        {"Total deposits", ""},   {"Total withdrawals", ""},
        {"Interest earned", ""},  {"Tax withheld", ""},
    };
    format_sek(json_get_number(summary, "starting_balance_sek", 0.0),
               rows[0].value, sizeof(rows[0].value));
    format_sek(json_get_number(summary, "ending_balance_sek", 0.0),
               rows[1].value, sizeof(rows[1].value));
    format_sek(json_get_number(summary, "total_deposits_sek", 0.0),
               rows[2].value, sizeof(rows[2].value));
    format_sek(json_get_number(summary, "total_withdrawals_sek", 0.0),
               rows[3].value, sizeof(rows[3].value));
    format_sek(json_get_number(summary, "total_interest_earned_sek", 0.0),
               rows[4].value, sizeof(rows[4].value));
    format_sek(json_get_number(summary, "total_tax_withheld_sek", 0.0),
               rows[5].value, sizeof(rows[5].value));

    int result = draw_key_value_rows(ctx, rows, sizeof(rows) / sizeof(rows[0]));
    ctx->y -= 10.0f;
    return result;
}

/** @brief Draws the transaction table's bold column headings and a rule. */
static int draw_transactions_header(RenderCtx* ctx) {
    if (ensure_space(ctx, TABLE_ROW_HEIGHT * 2) != 0) {
        return -1;
    }
    draw_text_left(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_DATE_X, "Date");
    draw_text_left(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_TYPE_X, "Type");
    draw_text_left(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_DESC_X,
                   "Description");
    draw_text_right(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_AMOUNT_RIGHT_X,
                    "Amount");
    draw_text_right(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_BALANCE_RIGHT_X,
                    "Balance");
    ctx->y -= 4.0f;
    draw_rule(ctx);
    ctx->y -= TABLE_ROW_HEIGHT;
    return 0;
}

static void draw_transaction_row(RenderCtx* ctx, const cJSON* tx) {
    char amount_buf[32];
    char balance_buf[32];
    format_sek(json_get_number(tx, "amount_sek", 0.0), amount_buf,
               sizeof(amount_buf));
    format_sek(json_get_number(tx, "balance_after_sek", 0.0), balance_buf,
               sizeof(balance_buf));

    draw_text_left(ctx, ctx->font_regular, FONT_SIZE_BODY, COL_DATE_X,
                   json_get_string(tx, "date", "-"));
    draw_text_left(ctx, ctx->font_regular, FONT_SIZE_BODY, COL_TYPE_X,
                   json_get_string(tx, "type", "-"));
    draw_text_left(ctx, ctx->font_regular, FONT_SIZE_BODY, COL_DESC_X,
                   json_get_string(tx, "description", "-"));
    draw_text_right(ctx, ctx->font_regular, FONT_SIZE_BODY, COL_AMOUNT_RIGHT_X,
                    amount_buf);
    draw_text_right(ctx, ctx->font_regular, FONT_SIZE_BODY, COL_BALANCE_RIGHT_X,
                    balance_buf);
    ctx->y -= TABLE_ROW_HEIGHT;
}

/**
 * @brief Renders the transactions table, adding pages (and repeating the
 * column headings) as needed. The row count is entirely driven by however
 * many entries are in `transactions` - no fixed limit.
 */
static int draw_transactions_section(RenderCtx*   ctx,
                                     const cJSON* transactions) {
    if (draw_section_title(ctx, "Transactions") != 0) {
        return -1;
    }
    if (draw_transactions_header(ctx) != 0) {
        return -1;
    }

    const cJSON* tx = NULL;
    cJSON_ArrayForEach(tx, transactions) {
        if (ctx->y - TABLE_ROW_HEIGHT < CONTENT_BOTTOM) {
            if (start_new_page(ctx) != 0) {
                return -1;
            }
            if (draw_transactions_header(ctx) != 0) {
                return -1;
            }
        }
        draw_transaction_row(ctx, tx);
    }
    return 0;
}

int tax_report_layout(HPDF_Doc pdf, const cJSON* root) {
    if (!root) {
        LOG_ERROR("tax_report_layout called with no JSON data");
        return -1;
    }

    RenderCtx ctx = {.pdf = pdf};
    // WinAnsiEncoding (~CP1252) is required for å/ä/ö and other Latin-1
    // characters to render; paired with utf8_to_winansi() above.
    ctx.font_regular = HPDF_GetFont(pdf, "Helvetica", "WinAnsiEncoding");
    ctx.font_bold    = HPDF_GetFont(pdf, "Helvetica-Bold", "WinAnsiEncoding");
    if (!ctx.font_regular || !ctx.font_bold) {
        LOG_ERROR("Failed to load Helvetica fonts");
        return -2;
    }

    if (start_new_page(&ctx) != 0) {
        LOG_ERROR("Failed to create first page");
        return -3;
    }

    const cJSON* metadata = cJSON_GetObjectItemCaseSensitive(root, "metadata");
    const cJSON* customer = cJSON_GetObjectItemCaseSensitive(root, "customer");
    const cJSON* account  = cJSON_GetObjectItemCaseSensitive(root, "account");
    const cJSON* summary  = cJSON_GetObjectItemCaseSensitive(root, "summary");
    const cJSON* transactions =
        cJSON_GetObjectItemCaseSensitive(root, "transactions");

    if (draw_title_block(&ctx, metadata) != 0 ||
        draw_customer_section(&ctx, customer) != 0 ||
        draw_account_section(&ctx, account) != 0 ||
        draw_summary_section(&ctx, summary) != 0 ||
        draw_transactions_section(&ctx, transactions) != 0) {
        LOG_ERROR("Failed to lay out tax report");
        return -4;
    }

    return 0;
}
