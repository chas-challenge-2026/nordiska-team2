#pragma once

#include <cjson/cJSON.h>
#include <hpdf.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Result codes returned by PDF generation operations.
 */
typedef enum {
    PDF_SUCCESS = 0, /**< Operation completed successfully */
    PDF_ERROR_INVALID_ARGS =
        -1, /**< One or more required arguments were NULL or invalid */
    PDF_ERROR_OUT_OF_MEMORY =
        -2, /**< Failed to allocate libHaru document instance */
    PDF_ERROR_JSON_PARSE = -3, /**< Failed to parse the input JSON string */
    PDF_ERROR_LAYOUT_FAILED =
        -4, /**< Custom layout strategy function returned an error */
    PDF_ERROR_SAVE_FAILED = -5 /**< Failed to write the compiled PDF to disk */
} PdfResult;

/**
 * @brief Function signature for a PDF layout strategy.
 *
 * Implementors provide custom drawing logic here to populate the document.
 * @param pdf The libHaru document handle.
 * @param root The parsed JSON root node containing the layout data.
 * @return int Returns 0 on success, or a non-zero error code on failure.
 */
typedef int (*PdfLayoutFn)(HPDF_Doc pdf, const cJSON* root);

/**
 * @brief Generates a PDF using a custom layout function and optional JSON data
 * string.
 *
 * @param json_str Null-terminated JSON string (can be NULL if layout doesn't
 * need data).
 * @param output_path File path where the generated PDF should be saved.
 * @param layout_fn The layout function responsible for rendering the document
 * contents.
 * @return PdfResult Returns PDF_SUCCESS (0) on success, or a negative error
 * code on failure.
 */
int pdf_generator_generate(const char* json_str, const char* output_path,
                           PdfLayoutFn layout_fn);

#ifdef __cplusplus
}
#endif
