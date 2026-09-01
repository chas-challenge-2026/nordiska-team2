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
 * @brief Generates and optionally signs a PDF document from a JSON file.
 *
 * Renders the layout specified by `report_type` using data provided in
 * `json_file_path`. Optionally applies a digital PKCS#12 signature if a PFX
 * file is provided.
 *
 * @param[in] json_file_path Path to the JSON file on disk containing report
 * data (object or array).
 * @param[in] report_type    Layout type to apply.
 * @param[in] out_path       Destination path on disk for the generated PDF.
 * @param[in] pfx_path       Optional path to a .pfx/.p12 certificate for
 * signing. Pass NULL for unsigned.
 * @param[in] password       Optional password for the PFX certificate. Pass
 * NULL if unencrypted or unused.
 *
 * @return Returns 0 on success, or a negative error code on failure.
 */
PDF_ENGINE_API int pdf_engine_generate_and_sign(const char*   json_file_path,
                                                PdfReportType report_type,
                                                const char*   out_path,
                                                const char*   pfx_path,
                                                const char*   password);

#ifdef __cplusplus
}
#endif
