#include "layouts/pdf_render.h"

#include "assets/logo_data.h"
#include "logging/log.h"

#include <stdio.h>
#include <string.h>

const char* json_get_string(const cJSON* obj, const char* key,
                            const char* fallback) {
    const cJSON* item = cJSON_GetObjectItemCaseSensitive(obj, key);
    if (cJSON_IsString(item) && item->valuestring) {
        return item->valuestring;
    }
    return fallback;
}

double json_get_number(const cJSON* obj, const char* key, double fallback) {
    const cJSON* item = cJSON_GetObjectItemCaseSensitive(obj, key);
    if (cJSON_IsNumber(item)) {
        return item->valuedouble;
    }
    return fallback;
}

void format_sek(double amount, char* buf, size_t buf_len) {
    snprintf(buf, buf_len, "%.2f SEK", amount);
}

/** UTF-8 -> single-byte WinAnsi (CP1252); anything beyond Latin-1 becomes '?'.
 */
static void utf8_to_winansi(const char* utf8, char* out, size_t out_cap) {
    size_t out_len = 0;
    for (const unsigned char* p = (const unsigned char*)utf8;
         *p != '\0' && out_len + 1 < out_cap; p++) {
        if (*p < 0x80) {
            out[out_len++] = (char)*p;
        } else if ((*p & 0xE0) == 0xC0 && (p[1] & 0xC0) == 0x80) {
            unsigned int cp = ((unsigned int)(*p & 0x1F) << 6) | (p[1] & 0x3F);
            out[out_len++]  = (cp <= 0xFF) ? (char)(unsigned char)cp : '?';
            p++;
        } else {
            out[out_len++] = '?';
        }
    }
    out[out_len] = '\0';
}

/** Top-right corner, scaled to fit the LOGO_MAX_* box. */
static void draw_logo(RenderCtx* ctx) {
    if (!ctx->logo) {
        return;
    }
    float aspect = (float)LOGO_WIDTH / (float)LOGO_HEIGHT;
    float h      = LOGO_MAX_HEIGHT;
    float w      = h * aspect;
    if (w > LOGO_MAX_WIDTH) {
        w = LOGO_MAX_WIDTH;
        h = w / aspect;
    }
    // Top edge sits 10 pt into the top margin, right edge on the content edge.
    HPDF_Page_DrawImage(ctx->page, ctx->logo, CONTENT_RIGHT - w,
                        CONTENT_TOP + 10.0f - h, w, h);
}

int render_start_new_page(RenderCtx* ctx) {
    HPDF_Page page = HPDF_AddPage(ctx->pdf);
    if (!page) {
        return -1;
    }
    HPDF_Page_SetSize(page, HPDF_PAGE_SIZE_A4, HPDF_PAGE_PORTRAIT);
    ctx->page = page;
    ctx->y    = CONTENT_TOP;
    return 0;
}

int render_ctx_init(RenderCtx* ctx, HPDF_Doc pdf) {
    memset(ctx, 0, sizeof(*ctx));
    ctx->pdf          = pdf;
    ctx->font_regular = HPDF_GetFont(pdf, "Helvetica", "WinAnsiEncoding");
    ctx->font_bold    = HPDF_GetFont(pdf, "Helvetica-Bold", "WinAnsiEncoding");
    if (!ctx->font_regular || !ctx->font_bold) {
        LOG_ERROR("Failed to load Helvetica fonts");
        return -2;
    }

    // The pixel data is static; libharu copies it into this document only.
    ctx->logo = HPDF_LoadRawImageFromMem(pdf, LOGO_RGB, LOGO_WIDTH, LOGO_HEIGHT,
                                         HPDF_CS_DEVICE_RGB, 8);
    if (!ctx->logo) {
        LOG_ERROR("Failed to load logo; continuing without it");
        HPDF_ResetError(pdf); // don't let a logo failure poison the document
    }

    if (render_start_new_page(ctx) != 0) {
        LOG_ERROR("Failed to create first page");
        return -3;
    }
    draw_logo(ctx);
    return 0;
}

int render_ensure_space(RenderCtx* ctx, float needed_height) {
    if (ctx->y - needed_height >= CONTENT_BOTTOM) {
        return 0;
    }
    return render_start_new_page(ctx);
}

void render_text_left(RenderCtx* ctx, HPDF_Font font, float size, float x,
                      const char* text) {
    char encoded[256];
    utf8_to_winansi(text, encoded, sizeof(encoded));
    HPDF_Page_SetFontAndSize(ctx->page, font, size);
    HPDF_Page_BeginText(ctx->page);
    HPDF_Page_TextOut(ctx->page, x, ctx->y, encoded);
    HPDF_Page_EndText(ctx->page);
}

void render_text_right(RenderCtx* ctx, HPDF_Font font, float size,
                       float right_x, const char* text) {
    char encoded[256];
    utf8_to_winansi(text, encoded, sizeof(encoded));
    HPDF_Page_SetFontAndSize(ctx->page, font, size);
    float width = (float)HPDF_Page_TextWidth(ctx->page, encoded);
    HPDF_Page_BeginText(ctx->page);
    HPDF_Page_TextOut(ctx->page, right_x - width, ctx->y, encoded);
    HPDF_Page_EndText(ctx->page);
}

void render_rule(RenderCtx* ctx) {
    HPDF_Page_SetLineWidth(ctx->page, 0.5f);
    HPDF_Page_MoveTo(ctx->page, MARGIN_LEFT, ctx->y);
    HPDF_Page_LineTo(ctx->page, CONTENT_RIGHT, ctx->y);
    HPDF_Page_Stroke(ctx->page);
}

int render_title_block(RenderCtx* ctx, const char* title,
                       const char* subtitle) {
    if (render_ensure_space(ctx, 60.0f) != 0) {
        return -1;
    }
    render_text_left(ctx, ctx->font_bold, FONT_SIZE_TITLE, MARGIN_LEFT, title);
    ctx->y -= 20.0f;
    render_text_left(ctx, ctx->font_regular, FONT_SIZE_SUBTITLE, MARGIN_LEFT,
                     subtitle);
    ctx->y -= LINE_HEIGHT_SECTION_GAP;
    return 0;
}

int render_section_title(RenderCtx* ctx, const char* title) {
    if (render_ensure_space(ctx, LINE_HEIGHT_SECTION_GAP + LINE_HEIGHT_BODY) !=
        0) {
        return -1;
    }
    render_text_left(ctx, ctx->font_bold, FONT_SIZE_SECTION, MARGIN_LEFT,
                     title);
    ctx->y -= 4.0f;
    render_rule(ctx);
    ctx->y -= LINE_HEIGHT_BODY;
    return 0;
}

int render_key_value_rows(RenderCtx* ctx, const KeyValueRow* rows,
                          size_t row_count) {
    const float LABEL_X = MARGIN_LEFT;
    const float VALUE_X = MARGIN_LEFT + 150.0f;

    for (size_t i = 0; i < row_count; i++) {
        if (render_ensure_space(ctx, LINE_HEIGHT_BODY) != 0) {
            return -1;
        }
        char label[64];
        snprintf(label, sizeof(label), "%s:", rows[i].label);
        render_text_left(ctx, ctx->font_bold, FONT_SIZE_BODY, LABEL_X, label);
        render_text_left(ctx, ctx->font_regular, FONT_SIZE_BODY, VALUE_X,
                         rows[i].value);
        ctx->y -= LINE_HEIGHT_BODY;
    }
    return 0;
}

static int draw_transactions_header(RenderCtx* ctx) {
    if (render_ensure_space(ctx, TABLE_ROW_HEIGHT * 2) != 0) {
        return -1;
    }
    render_text_left(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_DATE_X, "Date");
    render_text_left(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_TYPE_X, "Type");
    render_text_left(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_DESC_X,
                     "Description");
    render_text_right(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_AMOUNT_RIGHT_X,
                      "Amount");
    render_text_right(ctx, ctx->font_bold, FONT_SIZE_BODY, COL_BALANCE_RIGHT_X,
                      "Balance");
    ctx->y -= 4.0f;
    render_rule(ctx);
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

    render_text_left(ctx, ctx->font_regular, FONT_SIZE_BODY, COL_DATE_X,
                     json_get_string(tx, "date", "-"));
    render_text_left(ctx, ctx->font_regular, FONT_SIZE_BODY, COL_TYPE_X,
                     json_get_string(tx, "type", "-"));
    render_text_left(ctx, ctx->font_regular, FONT_SIZE_BODY, COL_DESC_X,
                     json_get_string(tx, "description", "-"));
    render_text_right(ctx, ctx->font_regular, FONT_SIZE_BODY,
                      COL_AMOUNT_RIGHT_X, amount_buf);
    render_text_right(ctx, ctx->font_regular, FONT_SIZE_BODY,
                      COL_BALANCE_RIGHT_X, balance_buf);
    ctx->y -= TABLE_ROW_HEIGHT;
}

int render_transactions_section(RenderCtx* ctx, const cJSON* transactions) {
    if (render_section_title(ctx, "Transactions") != 0 ||
        draw_transactions_header(ctx) != 0) {
        return -1;
    }
    const cJSON* tx = NULL;
    cJSON_ArrayForEach(tx, transactions) {
        if (ctx->y - TABLE_ROW_HEIGHT < CONTENT_BOTTOM) {
            if (render_start_new_page(ctx) != 0 ||
                draw_transactions_header(ctx) != 0) {
                return -1;
            }
        }
        draw_transaction_row(ctx, tx);
    }
    return 0;
}
