#pragma once

#include <stddef.h>

#ifdef _WIN32
#    define PDF_ENGINE_API __declspec(dllexport)
#else
#    define PDF_ENGINE_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Known report layout targets supported by the PDF engine.
 */
typedef enum {
    PDF_REPORT_TYPE_TAX_REPORT = 0, /**< Official Annual Tax Report layout */
} PdfReportType;

/**
 * @brief Generates and optionally signs PDF documents from a structured JSON
 * payload.
 *
 * This function handles multi-report JSON payloads (arrays or single report
 * objects), renders the layout pages according to the specified report type,
 * and optionally applies a digital PKCS#12 signature.
 *
 * @param json_data A null-terminated UTF-8 JSON payload (object or array of
 * reports).
 * @param report_type The report layout strategy to apply, or
 * PDF_REPORT_TYPE_AUTO.
 * @param out_path  Output path on disk for the compiled PDF file.
 * @param pfx_path  Optional path to a .pfx/.p12 signing certificate (pass NULL
 * if unsigned).
 * @param password  Optional password to unlock the PFX certificate (pass NULL
 * if unencrypted/unused).
 *
 * @return int Returns `0` (PDF_SUCCESS) on success, or a negative error code on
 * failure.
 */
PDF_ENGINE_API int pdf_engine_generate_and_sign(const char*   json_data,
                                                PdfReportType report_type,
                                                const char*   out_path,
                                                const char*   pfx_path,
                                                const char*   password);

#ifdef __cplusplus
}
#endif
