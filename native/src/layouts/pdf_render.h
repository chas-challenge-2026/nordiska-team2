#pragma once

#include <cjson/cJSON.h>
#include <hpdf.h>
#include <stddef.h>

// A4 in points; mirrors HPDF_Page_SetSize() so layout math needn't query it.
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

// The logo is scaled to fit inside this box, keeping its aspect ratio.
#define LOGO_MAX_WIDTH 90.0f
#define LOGO_MAX_HEIGHT 28.0f

#define COL_DATE_X MARGIN_LEFT
#define COL_TYPE_X (MARGIN_LEFT + 65.0f)
#define COL_DESC_X (MARGIN_LEFT + 135.0f)
#define COL_AMOUNT_RIGHT_X (MARGIN_LEFT + 380.0f)
#define COL_BALANCE_RIGHT_X CONTENT_RIGHT

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    const char* label;
    char        value[160];
} KeyValueRow;

typedef struct {
    HPDF_Doc   pdf;
    HPDF_Page  page;
    HPDF_Font  font_regular;
    HPDF_Font  font_bold;
    HPDF_Image logo; /**< NULL if loading failed; the report still renders */
    float      y;
} RenderCtx;

const char* json_get_string(const cJSON* obj, const char* key,
                            const char* fallback);
double      json_get_number(const cJSON* obj, const char* key, double fallback);
void        format_sek(double amount, char* buf, size_t buf_len);

/** Loads fonts + logo, creates page 1 and draws the logo on it. */
int render_ctx_init(RenderCtx* ctx, HPDF_Doc pdf);
int render_start_new_page(RenderCtx* ctx);
int render_ensure_space(RenderCtx* ctx, float needed_height);

void render_text_left(RenderCtx* ctx, HPDF_Font font, float size, float x,
                      const char* text);
void render_text_right(RenderCtx* ctx, HPDF_Font font, float size,
                       float right_x, const char* text);
void render_rule(RenderCtx* ctx);

int render_title_block(RenderCtx* ctx, const char* title, const char* subtitle);
int render_section_title(RenderCtx* ctx, const char* title);
int render_key_value_rows(RenderCtx* ctx, const KeyValueRow* rows,
                          size_t row_count);
int render_transactions_section(RenderCtx* ctx, const cJSON* transactions);

#ifdef __cplusplus
}
#endif
