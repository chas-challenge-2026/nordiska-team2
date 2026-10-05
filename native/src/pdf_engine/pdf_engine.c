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
#include "layouts/bank_statement.h"
#include "layouts/pdf_layout.h"
#include "layouts/tax_report.h"
#include "logging/log.h"
#include "signer/pdf_signer.h"

#define REPORT_ID_MAX 96
#define OUT_PATH_MAX 512
#define DETAIL_MAX 192
#define FIELD_SEG_MAX 64
#define FIELD_PATH_MAX 128

/** @brief State threaded through json_stream_array_objects() for one batch. */
typedef struct {
    const char*            out_dir;
    const PdfLayoutConfig* config;
    PdfSigner*             signer;
    PdfEngineProgressCb    progress_cb;
    void*                  progress_ctx;
    long object_index; /**< Position in the array; used for reporting ids */
    int  success_count;
    int  saw_any_object;
} BatchState;

/** @brief Maps a PdfReportType to its layout configuration bundle. */
static const PdfLayoutConfig* resolve_layout_config(PdfReportType report_type) {
    switch (report_type) {
    case PDF_REPORT_TYPE_TAX_REPORT:
        return tax_report_get_config();
    case PDF_REPORT_TYPE_BANK_STATEMENT:
        return bank_statement_get_config();
    default:
        return NULL;
    }
}

/** @brief Walks a dot-separated path from `node`. Returns NULL if any
 * segment is absent. */
static const cJSON* resolve_path(const cJSON* node, const char* path) {
    char seg[FIELD_SEG_MAX];
    while (node && *path) {
        const char* dot = strchr(path, '.');
        size_t      len = dot ? (size_t)(dot - path) : strlen(path);
        if (len == 0 || len >= sizeof(seg)) {
            return NULL;
        }
        memcpy(seg, path, len);
        seg[len] = '\0';
        node     = cJSON_GetObjectItemCaseSensitive(node, seg);
        path     = dot ? dot + 1 : path + len;
    }
    return node;
}

static int type_matches(const cJSON* item, PdfFieldType type) {
    switch (type) {
    case PDF_FIELD_STRING:
        return cJSON_IsString(item) && item->valuestring &&
               item->valuestring[0] != '\0';
    case PDF_FIELD_NUMBER:
        return cJSON_IsNumber(item);
    case PDF_FIELD_ARRAY:
        return cJSON_IsArray(item);
    case PDF_FIELD_OBJECT:
        return cJSON_IsObject(item);
    }
    return 0;
}

static const char* type_name(PdfFieldType type) {
    switch (type) {
    case PDF_FIELD_STRING:
        return "non-empty string";
    case PDF_FIELD_NUMBER:
        return "number";
    case PDF_FIELD_ARRAY:
        return "array";
    case PDF_FIELD_OBJECT:
        return "object";
    }
    return "?";
}

/**
 * @brief Checks every required field of `cfg` against `root`.
 * @return 0 if all present and correctly typed; -1 on the first problem,
 * with a description written to `detail`.
 */
static int validate_report(const cJSON* root, const PdfLayoutConfig* cfg,
                           char* detail, size_t detail_sz) {
    if (!cJSON_IsObject(root)) {
        snprintf(detail, detail_sz, "report is not a JSON object");
        return -1;
    }

    for (size_t i = 0; i < cfg->required_field_count; i++) {
        const PdfRequiredField* f    = &cfg->required_fields[i];
        const char*             wild = strstr(f->path, "[].");

        if (!wild) {
            if (!type_matches(resolve_path(root, f->path), f->type)) {
                snprintf(detail, detail_sz,
                         "missing or wrong type: %s (expected %s)", f->path,
                         type_name(f->type));
                return -1;
            }
            continue;
        }

        // "transactions[].amount_sek": check the suffix on every element.
        size_t prefix_len = (size_t)(wild - f->path);
        char   arr_path[FIELD_PATH_MAX];
        if (prefix_len >= sizeof(arr_path)) {
            snprintf(detail, detail_sz, "bad field path in layout: %s",
                     f->path);
            return -1;
        }
        memcpy(arr_path, f->path, prefix_len);
        arr_path[prefix_len] = '\0';

        const cJSON* arr = resolve_path(root, arr_path);
        if (!cJSON_IsArray(arr)) {
            snprintf(detail, detail_sz,
                     "missing or wrong type: %s (expected array)", arr_path);
            return -1;
        }

        const char*  suffix = wild + 3;
        int          idx    = 0;
        const cJSON* el     = NULL;
        cJSON_ArrayForEach(el, arr) {
            if (!type_matches(resolve_path(el, suffix), f->type)) {
                snprintf(detail, detail_sz,
                         "missing or wrong type: %s[%d].%s (expected %s)",
                         arr_path, idx, suffix, type_name(f->type));
                return -1;
            }
            idx++;
        }
    }
    return 0;
}

/** @brief Renders, optionally signs, and atomically publishes one PDF. */
static int render_and_publish(const char* json_obj_str, const BatchState* batch,
                              const char* report_id) {
    char out_path[OUT_PATH_MAX];
    char tmp_path[OUT_PATH_MAX + 4];
    snprintf(out_path, sizeof(out_path), "%s/%s.pdf", batch->out_dir,
             report_id);
    snprintf(tmp_path, sizeof(tmp_path), "%s.tmp", out_path);

    int gen_result = pdf_generator_generate(json_obj_str, tmp_path,
                                            batch->config->layout_fn);
    if (gen_result != PDF_SUCCESS) {
        LOG_ERROR("Generation failed for '%s' (pdf_generator status=%d)",
                  out_path, gen_result);
        remove(tmp_path);
        return PDF_ENGINE_ERROR_GENERATION_FAILED;
    }

    if (batch->signer) {
        int sign_result = pdf_signer_sign(batch->signer, tmp_path, tmp_path);
        if (sign_result != 0) {
            LOG_ERROR("Signing failed for '%s' (pdf_signer status=%d)",
                      out_path, sign_result);
            remove(tmp_path);
            return PDF_ENGINE_ERROR_SIGNING_FAILED;
        }
    }

    if (replace_file(tmp_path, out_path) != 0) {
        LOG_ERROR("Failed to move temp file to '%s'", out_path);
        remove(tmp_path);
        return PDF_ENGINE_ERROR_GENERATION_FAILED;
    }
    return PDF_ENGINE_SUCCESS;
}

/** @brief Validates, then generates and optionally signs one report. */
static void generate_one_report(const char* json_obj_str, BatchState* batch) {
    char report_id[REPORT_ID_MAX] = {0};
    char detail[DETAIL_MAX]       = {0};
    int  status                   = PDF_ENGINE_SUCCESS;

    cJSON* root = cJSON_Parse(json_obj_str);
    if (!root) {
        status = PDF_ENGINE_ERROR_MALFORMED_REPORT;
        snprintf(detail, sizeof(detail), "report is not valid JSON");
    } else if (validate_report(root, batch->config, detail, sizeof(detail)) !=
               0) {
        status = PDF_ENGINE_ERROR_MISSING_FIELD;
    } else {
        if (batch->config->extract_id_fn) {
            batch->config->extract_id_fn(root, report_id, sizeof(report_id));
        }
        if (report_id[0] == '\0') {
            status = PDF_ENGINE_ERROR_MISSING_FIELD;
            snprintf(detail, sizeof(detail),
                     "no usable report identifier (needed for filename)");
        }
    }

    if (status == PDF_ENGINE_SUCCESS) {
        status = render_and_publish(json_obj_str, batch, report_id);
        if (status != PDF_ENGINE_SUCCESS) {
            snprintf(detail, sizeof(detail), "%s",
                     status == PDF_ENGINE_ERROR_SIGNING_FAILED
                         ? "signing failed"
                         : "PDF generation failed");
        } else {
            batch->success_count++;
        }
    } else {
        LOG_ERROR("Report #%ld rejected: %s", batch->object_index, detail);
    }

    // Positional id is for reporting only when there is no real identifier;
    // nothing is written to disk under it.
    if (report_id[0] == '\0') {
        snprintf(report_id, sizeof(report_id), "report_%ld",
                 batch->object_index);
    }

    if (batch->progress_cb) {
        PdfEngineReportResult result = {
            .report_id = report_id, .status = status, .detail = detail};
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
