#pragma once

#include <cjson/cJSON.h>
#include <hpdf.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef int (*PdfLayoutFn)(HPDF_Doc pdf, const cJSON* root);
typedef void (*PdfIdExtractorFn)(const cJSON* root, char* out, size_t out_cap);

/** @brief JSON type a required field must have. */
typedef enum {
    PDF_FIELD_STRING, /**< string, and non-empty */
    PDF_FIELD_NUMBER,
    PDF_FIELD_ARRAY, /**< may be empty; presence + type only */
    PDF_FIELD_OBJECT,
} PdfFieldType;

/**
 * @brief One field a report object must contain before it is rendered.
 *
 * `path` is a dot-separated path from the report root, e.g.
 * "account.account_number". A "[]." segment applies the rest of the path to
 * every element of an array, e.g. "transactions[].amount_sek".
 */
typedef struct {
    const char*  path;
    PdfFieldType type;
} PdfRequiredField;

/**
 * @brief Bundles a rendering function, an ID extraction strategy and the
 * input contract for a report type.
 *
 * Anything the layout draws from the JSON must be listed in
 * `required_fields`, or be deliberately optional and handled by the layout.
 */
typedef struct {
    PdfLayoutFn             layout_fn;
    PdfIdExtractorFn        extract_id_fn;
    const PdfRequiredField* required_fields;
    size_t                  required_field_count;
} PdfLayoutConfig;

#ifdef __cplusplus
}
#endif
