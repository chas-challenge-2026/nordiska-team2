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
    PDF_REPORT_TYPE_BANK_STATEMENT = 1, /**< Account statement (kontoutdrag) */
} PdfReportType;

/**
 * @brief Result codes returned by the batch functions themselves (whole-batch,
 * fatal failures), and reported per report through
 * PdfEngineReportResult::status.
 */
typedef enum {
    PDF_ENGINE_SUCCESS = 0,
    PDF_ENGINE_ERROR_INVALID_ARGS =
        -1, /**< A required argument was NULL/empty (for
             * pdf_engine_generate_and_sign this includes pfx_path) */
    PDF_ENGINE_ERROR_NO_REPORT_OBJECT =
        -2, /**< The JSON file's root array had no objects */
    PDF_ENGINE_ERROR_JSON_READ_FAILED =
        -3, /**< The JSON file couldn't be opened or was malformed */
    PDF_ENGINE_ERROR_OUT_OF_MEMORY = -4,
    PDF_ENGINE_ERROR_UNKNOWN_REPORT_TYPE =
        -5, /**< No layout is registered for `report_type` */
    PDF_ENGINE_ERROR_GENERATION_FAILED =
        -6, /**< PDF layout/rendering failed (per-report) */
    PDF_ENGINE_ERROR_SIGNING_FAILED =
        -7, /**< Signing failed, or the output did not contain a signature
             * (per-report), or the PFX could not be loaded (whole-batch) */
    PDF_ENGINE_ERROR_MISSING_FIELD =
        -8, /**< Input data is incomplete or has a wrong-typed field
             * (per-report). See PdfEngineReportResult::detail. */
    PDF_ENGINE_ERROR_MALFORMED_REPORT =
        -9, /**< A single report object isn't a valid JSON object
             * (per-report). */
} PdfEngineResult;

/**
 * @brief Outcome of generating a single report within a batch, passed to a
 * PdfEngineProgressCb.
 */
typedef struct {
    /** Filename (without directory or ".pdf" extension) the report was
     * written under - sanitized, so it may not exactly match the source
     * JSON's identifier field. If the report had no usable identifier this
     * is a positional name ("report_<index>") used for reporting only; no
     * file is written in that case. Valid only for the duration of the
     * callback; copy it if you need it afterwards. */
    const char* report_id;
    /** PDF_ENGINE_SUCCESS, or the PdfEngineResult explaining why this one
     * report failed. A per-report failure never aborts the rest of the
     * batch. */
    int status;
    /** Human-readable reason for a failure, e.g. "missing or wrong type:
     * account.account_number (expected string)". Empty string on success.
     * Never NULL. Valid only for the duration of the callback. */
    const char* detail;
} PdfEngineReportResult;

/**
 * @brief Called once per report object as the batch is streamed, after that
 * report has either been written to disk or failed. Optional - pass NULL if
 * you don't need per-report visibility.
 */
typedef void (*PdfEngineProgressCb)(const PdfEngineReportResult* result,
                                    void*                        user_ctx);

/**
 * @brief Streams a JSON array of reports and writes one SIGNED PDF per report
 * into `out_dir`.
 *
 * `json_file_path` must contain a root-level JSON array of report objects.
 * Objects are read and rendered one at a time, so memory use stays O(1) in
 * the number of reports.
 *
 * Signing is mandatory: `pfx_path` must be non-empty, otherwise this returns
 * PDF_ENGINE_ERROR_INVALID_ARGS and writes nothing. A report is only
 * published if the finished file verifiably contains a signature; if signing
 * fails or leaves no signature behind, no PDF is written for that report and
 * it is reported as PDF_ENGINE_ERROR_SIGNING_FAILED.
 *
 * Each report is validated against its layout's required fields before
 * anything is rendered. A report with a missing or wrong-typed required
 * field, or with no usable identifier, is rejected (see
 * PDF_ENGINE_ERROR_MISSING_FIELD / PDF_ENGINE_ERROR_MALFORMED_REPORT).
 *
 * Each valid report is written to `out_dir/<id>.pdf`. `out_dir` is created
 * if it doesn't exist (a single directory level - its parent must exist).
 *
 * A single report failing does NOT stop the batch. Pass `progress_cb` to
 * find out which reports failed and why - the return value alone only tells
 * you how many succeeded.
 *
 * @param[in] json_file_path Path to a JSON file with a root-level array.
 * @param[in] report_type    Layout type to apply to every report.
 * @param[in] out_dir        Destination directory for the generated PDFs.
 * @param[in] pfx_path       REQUIRED path to a .pfx/.p12 certificate.
 * @param[in] password       Password for the PFX. NULL if unencrypted.
 * @param[in] progress_cb    Optional per-report callback.
 * @param[in] progress_ctx   Opaque pointer passed through to `progress_cb`.
 *
 * @return On success, the number of reports successfully generated and
 * signed (>= 0; 0 can mean every report failed individually). On a fatal,
 * whole-batch failure, a negative PdfEngineResult. Callers must treat
 * `< 0` as an error.
 */
PDF_ENGINE_API int pdf_engine_generate_and_sign(
    const char* json_file_path, PdfReportType report_type, const char* out_dir,
    const char* pfx_path, const char* password, PdfEngineProgressCb progress_cb,
    void* progress_ctx);

/**
 * @brief Same as pdf_engine_generate_and_sign() but produces UNSIGNED PDFs.
 * Intended for development and testing only.
 */
PDF_ENGINE_API int pdf_engine_generate_pdfs(const char*         json_file_path,
                                            PdfReportType       report_type,
                                            const char*         out_dir,
                                            PdfEngineProgressCb progress_cb,
                                            void*               progress_ctx);

#ifdef __cplusplus
}
#endif
