#include "pdf_engine.h"

#include "utils/utils.h"

#include <errno.h>
#include <stdio.h>
#include <string.h>

#ifdef _WIN32
#    include <direct.h>
#else
#    include <sys/stat.h>
#    include <sys/types.h>
#endif

#include "generator/pdf_generator.h"
#include "json_streamer/json_streamer.h"
#include "layouts/tax_report.h"
#include "logging/log.h"
#include "signer/pdf_signer.h"

#define REPORT_ID_MAX 96
#define OUT_PATH_MAX 512

/** @brief State threaded through json_stream_array_objects() for one batch. */
typedef struct {
    const char*         out_dir;
    PdfLayoutFn         layout_fn;
    const char*         pfx_path;
    const char*         password;
    PdfEngineProgressCb progress_cb;
    void*               progress_ctx;
    long object_index; /**< Position in the array; used for fallback ids */
    int  success_count;
    int  saw_any_object;
} BatchState;

/** @brief Maps a PdfReportType to its layout rendering function. */
static PdfLayoutFn resolve_layout_fn(PdfReportType report_type) {
    switch (report_type) {
    case PDF_REPORT_TYPE_TAX_REPORT:
        return tax_report_layout;
    default:
        return NULL;
    }
}

/**
 * @brief Copies `raw` into `out`, replacing anything outside [A-Za-z0-9_-]
 * with '_'.
 *
 * `raw` comes straight from the input JSON, so treating it as a trusted
 * filename component would let a malformed or malicious record write
 * outside `out_dir` (e.g. via "../"). Stripping everything but a safe
 * character set neutralizes that regardless of what `raw` contains.
 */
static void sanitize_filename_component(const char* raw, char* out,
                                        size_t out_cap) {
    size_t out_len = 0;
    for (const char* p = raw;
         raw != NULL && *p != '\0' && out_len + 1 < out_cap; p++) {
        char c    = *p;
        int  safe = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                    (c >= '0' && c <= '9') || c == '-' || c == '_';
        if (safe) {
            out[out_len] = c;
        } else {
            out[out_len] = '_';
        }
        out_len++;
    }
    out[out_len] = '\0';
}

/**
 * @brief Derives a filesystem-safe report id from a report object.
 *
 * Tries, in order, the account number, the customer id, then the report id
 * from metadata - whichever is present first. Falls back to a positional
 * name ("report_<n>") if none are, or if the JSON couldn't be parsed at all.
 */
static void derive_report_id(const cJSON* root, long fallback_index, char* out,
                             size_t out_cap) {
    const char* candidate = NULL;
    if (root) {
        const cJSON* account =
            cJSON_GetObjectItemCaseSensitive(root, "account");
        const cJSON* customer =
            cJSON_GetObjectItemCaseSensitive(root, "customer");
        const cJSON* metadata =
            cJSON_GetObjectItemCaseSensitive(root, "metadata");

        const cJSON* account_number =
            cJSON_GetObjectItemCaseSensitive(account, "account_number");
        const cJSON* customer_id =
            cJSON_GetObjectItemCaseSensitive(customer, "customer_id");
        const cJSON* report_id =
            cJSON_GetObjectItemCaseSensitive(metadata, "report_id");

        if (cJSON_IsString(account_number) && account_number->valuestring[0]) {
            candidate = account_number->valuestring;
        } else if (cJSON_IsString(customer_id) && customer_id->valuestring[0]) {
            candidate = customer_id->valuestring;
        } else if (cJSON_IsString(report_id) && report_id->valuestring[0]) {
            candidate = report_id->valuestring;
        }
    }

    if (candidate) {
        sanitize_filename_component(candidate, out, out_cap);
    }
    if (!candidate || out[0] == '\0') {
        snprintf(out, out_cap, "report_%ld", fallback_index);
    }
}

/** @brief Generates and optionally signs one report, reporting the outcome. */
static void generate_one_report(const char* json_obj_str, BatchState* batch) {
    cJSON* root = cJSON_Parse(json_obj_str);

    char report_id[REPORT_ID_MAX];
    derive_report_id(root, batch->object_index, report_id, sizeof(report_id));

    char out_path[OUT_PATH_MAX];
    snprintf(out_path, sizeof(out_path), "%s/%s.pdf", batch->out_dir,
             report_id);

    int status = PDF_ENGINE_SUCCESS;
    int gen_result =
        pdf_generator_generate(json_obj_str, out_path, batch->layout_fn);
    if (gen_result != PDF_SUCCESS) {
        LOG_ERROR("Generation failed for '%s' (pdf_generator status=%d)",
                  out_path, gen_result);
        status = PDF_ENGINE_ERROR_GENERATION_FAILED;
    } else if (batch->pfx_path && *batch->pfx_path) {
        int sign_result = pdf_signer_sign(out_path, out_path, batch->pfx_path,
                                          batch->password);
        if (sign_result != 0) {
            LOG_ERROR("Signing failed for '%s' (pdf_signer status=%d)",
                      out_path, sign_result);
            status = PDF_ENGINE_ERROR_SIGNING_FAILED;
        }
    }

    if (status == PDF_ENGINE_SUCCESS) {
        batch->success_count++;
    }
    if (batch->progress_cb) {
        PdfEngineReportResult result = {.report_id = report_id,
                                        .status    = status};
        batch->progress_cb(&result, batch->progress_ctx);
    }

    cJSON_Delete(root);
}

static int batch_object_cb(const char* json_obj_str, void* user_ctx) {
    BatchState* batch     = (BatchState*)user_ctx;
    batch->saw_any_object = 1;

    generate_one_report(json_obj_str, batch);

    batch->object_index++;
    return 0; // never abort the batch over one report's failure
}

int pdf_engine_generate_and_sign(const char*   json_file_path,
                                 PdfReportType report_type, const char* out_dir,
                                 const char* pfx_path, const char* password,
                                 PdfEngineProgressCb progress_cb,
                                 void*               progress_ctx) {
    if (!json_file_path || !*json_file_path || !out_dir || !*out_dir) {
        LOG_ERROR("pdf_engine_generate_and_sign called with invalid arguments");
        return PDF_ENGINE_ERROR_INVALID_ARGS;
    }

    PdfLayoutFn layout_fn = resolve_layout_fn(report_type);
    if (!layout_fn) {
        LOG_ERROR("No layout registered for report_type=%d", (int)report_type);
        return PDF_ENGINE_ERROR_UNKNOWN_REPORT_TYPE;
    }

    if (ensure_directory_exists(out_dir) != 0) {
        LOG_ERROR("Could not create output directory '%s'", out_dir);
        return PDF_ENGINE_ERROR_GENERATION_FAILED;
    }

    BatchState batch = {
        .out_dir        = out_dir,
        .layout_fn      = layout_fn,
        .pfx_path       = pfx_path,
        .password       = password,
        .progress_cb    = progress_cb,
        .progress_ctx   = progress_ctx,
        .object_index   = 0,
        .success_count  = 0,
        .saw_any_object = 0,
    };

    int stream_status =
        json_stream_array_objects(json_file_path, batch_object_cb, &batch);
    if (stream_status != 0) {
        LOG_ERROR("Failed to read '%s' (json_streamer status=%d)",
                  json_file_path, stream_status);
        return PDF_ENGINE_ERROR_JSON_READ_FAILED;
    }
    if (!batch.saw_any_object) {
        LOG_ERROR("'%s' contains no report objects", json_file_path);
        return PDF_ENGINE_ERROR_NO_REPORT_OBJECT;
    }

    LOG_INFO("Batch complete: %d/%ld reports generated into '%s'",
             batch.success_count, batch.object_index, out_dir);
    return batch.success_count;
}
