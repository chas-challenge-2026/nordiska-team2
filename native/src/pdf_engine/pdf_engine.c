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
#include "layouts/pdf_layout.h"
#include "layouts/tax_report.h"
#include "logging/log.h"
#include "signer/pdf_signer.h"

#define REPORT_ID_MAX 96
#define OUT_PATH_MAX 512

/** @brief State threaded through json_stream_array_objects() for one batch. */
typedef struct {
    const char*            out_dir;
    const PdfLayoutConfig* config;
    PdfSigner*             signer;
    PdfEngineProgressCb    progress_cb;
    void*                  progress_ctx;
    long object_index; /**< Position in the array; used for fallback ids */
    int  success_count;
    int  saw_any_object;
} BatchState;

/** @brief Maps a PdfReportType to its layout configuration bundle. */
static const PdfLayoutConfig* resolve_layout_config(PdfReportType report_type) {
    switch (report_type) {
    case PDF_REPORT_TYPE_TAX_REPORT:
        return tax_report_get_config();
    default:
        return NULL;
    }
}

/** @brief Generates and optionally signs one report using a temporary file. */
static void generate_one_report(const char* json_obj_str, BatchState* batch) {
    cJSON* root = cJSON_Parse(json_obj_str);

    char report_id[REPORT_ID_MAX] = {0};
    if (root && batch->config->extract_id_fn) {
        batch->config->extract_id_fn(root, report_id, sizeof(report_id));
    }

    if (report_id[0] == '\0') {
        snprintf(report_id, sizeof(report_id), "report_%ld",
                 batch->object_index);
    }

    char out_path[OUT_PATH_MAX];
    char tmp_path[OUT_PATH_MAX + 4];
    snprintf(out_path, sizeof(out_path), "%s/%s.pdf", batch->out_dir,
             report_id);
    snprintf(tmp_path, sizeof(tmp_path), "%s.tmp", out_path);

    int status     = PDF_ENGINE_SUCCESS;
    int gen_result = pdf_generator_generate(json_obj_str, tmp_path,
                                            batch->config->layout_fn);

    if (gen_result != PDF_SUCCESS) {
        LOG_ERROR("Generation failed for '%s' (pdf_generator status=%d)",
                  out_path, gen_result);
        status = PDF_ENGINE_ERROR_GENERATION_FAILED;
        remove(tmp_path);
    } else if (batch->signer) {
        int sign_result = pdf_signer_sign(batch->signer, tmp_path, tmp_path);
        if (sign_result != 0) {
            LOG_ERROR("Signing failed for '%s' (pdf_signer status=%d)",
                      out_path, sign_result);
            status = PDF_ENGINE_ERROR_SIGNING_FAILED;
            remove(tmp_path);
        }
    }

    if (status == PDF_ENGINE_SUCCESS) {
        if (replace_file(tmp_path, out_path) != 0) {
            LOG_ERROR("Failed to move temp file to '%s'", out_path);
            status = PDF_ENGINE_ERROR_GENERATION_FAILED;
            remove(tmp_path);
        } else {
            batch->success_count++;
        }
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

    const PdfLayoutConfig* config = resolve_layout_config(report_type);
    if (!config) {
        LOG_ERROR("No layout registered for report_type=%d", (int)report_type);
        return PDF_ENGINE_ERROR_UNKNOWN_REPORT_TYPE;
    }

    if (ensure_directory_exists(out_dir) != 0) {
        LOG_ERROR("Could not create output directory '%s'", out_dir);
        return PDF_ENGINE_ERROR_GENERATION_FAILED;
    }

    PdfSigner* signer = NULL;
    if (pfx_path && *pfx_path) {
        signer = pdf_signer_create(pfx_path, password);
        if (!signer) {
            LOG_ERROR("Failed to initialize signer from PFX file '%s'",
                      pfx_path);
            return PDF_ENGINE_ERROR_SIGNING_FAILED;
        }
    }

    BatchState batch = {
        .out_dir        = out_dir,
        .config         = config,
        .signer         = signer,
        .progress_cb    = progress_cb,
        .progress_ctx   = progress_ctx,
        .object_index   = 0,
        .success_count  = 0,
        .saw_any_object = 0,
    };

    int stream_status =
        json_stream_array_objects(json_file_path, batch_object_cb, &batch);

    if (signer) {
        pdf_signer_free(signer);
    }

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
