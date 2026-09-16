#pragma once

#include <cjson/cJSON.h>
#include <hpdf.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef int (*PdfLayoutFn)(HPDF_Doc pdf, const cJSON* root);
typedef void (*PdfIdExtractorFn)(const cJSON* root, char* out, size_t out_cap);

/**
 * @brief Bundles a rendering function and an ID extraction strategy for a
 * report type.
 */
typedef struct {
    PdfLayoutFn      layout_fn;
    PdfIdExtractorFn extract_id_fn;
} PdfLayoutConfig;

#ifdef __cplusplus
}
#endif
