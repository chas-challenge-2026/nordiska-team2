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
 * @brief Result codes returned by pdf_engine_generate_and_sign() itself
 * (whole-batch, fatal failures). Per-report outcomes are reported through
 * PdfEngineReportResult instead - see pdf_engine_generate_and_sign()'s docs.
 */
typedef enum {
    PDF_ENGINE_SUCCESS = 0,
    PDF_ENGINE_ERROR_INVALID_ARGS =
        -1, /**< A required argument was NULL/empty */
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
        -7, /**< A PFX was supplied but signing failed (per-report) */
} PdfEngineResult;

/**
 * @brief Outcome of generating a single report within a batch, passed to a
 * PdfEngineProgressCb.
 */
typedef struct {
    /** Filename (without directory or ".pdf" extension) the report was, or
     * would have been, written under - sanitized, so it may not exactly
     * match the source JSON's identifier field. Valid only for the
     * duration of the callback; copy it if you need it afterwards. */
    const char* report_id;
    /** PDF_ENGINE_SUCCESS, or the PdfEngineResult explaining why this one
     * report failed. A per-report failure never aborts the rest of the
     * batch. */
    int status;
} PdfEngineReportResult;

/**
 * @brief Called once per report object as the batch is streamed, after that
 * report has either been written to disk or failed. Optional - pass NULL to
 * pdf_engine_generate_and_sign() if you don't need per-report visibility.
 */
typedef void (*PdfEngineProgressCb)(const PdfEngineReportResult* result,
                                    void*                        user_ctx);

/**
 * @brief Streams a JSON array of reports and writes one signed PDF per
 * report into `out_dir`.
 *
 * `json_file_path` must contain a root-level JSON array of report objects.
 * Objects are read and rendered one at a time via json_stream_array_objects()
 * so memory use stays O(1) in the number of reports, regardless of whether
 * the file holds one report or 10,000 - it's never loaded into memory whole.
 *
 * Each report is written to `out_dir/<id>.pdf`, where `<id>` is derived from
 * the report's account/customer identifier (sanitized to safe filename
 * characters; falls back to a positional name if no identifier is found).
 * `out_dir` is created if it doesn't already exist (a single directory
 * level - its parent must already exist).
 *
 * A single report failing to generate or sign does NOT stop the batch; every
 * other object in the array is still attempted. Pass `progress_cb` to find
 * out which reports, if any, failed and why - the return value alone only
 * tells you how many succeeded, not which ones.
 *
 * @param[in] json_file_path Path to a JSON file containing a root-level
 * array of report objects.
 * @param[in] report_type    Layout type to apply to every report.
 * @param[in] out_dir        Destination directory for the generated PDFs.
 * @param[in] pfx_path       Optional path to a .pfx/.p12 certificate used to
 * sign every generated PDF. Pass NULL (or an empty string) to leave them
 * unsigned.
 * @param[in] password       Optional password for the PFX certificate. Pass
 * NULL if unencrypted or unused.
 * @param[in] progress_cb    Optional callback invoked after each report is
 * attempted. Pass NULL if you don't need per-report results.
 * @param[in] progress_ctx   Opaque pointer passed through to `progress_cb`.
 *
 * @return On success, the number of reports successfully generated (>= 0,
 * and may be 0 if every report failed individually - check via
 * `progress_cb` when that distinction matters). On a fatal, whole-batch
 * failure (bad arguments, unreadable/malformed input file, empty array,
 * unknown report type), a negative PdfEngineResult.
 */
PDF_ENGINE_API int pdf_engine_generate_and_sign(
    const char* json_file_path, PdfReportType report_type, const char* out_dir,
    const char* pfx_path, const char* password, PdfEngineProgressCb progress_cb,
    void* progress_ctx);

#ifdef __cplusplus
}
#endif
