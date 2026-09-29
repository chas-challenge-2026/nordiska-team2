#include "pdf_engine.h"

#include <stdio.h>
#include <stdlib.h>
#include <sys/stat.h>
#include <sys/types.h>

#define OUT_DIR "data/output_pdfs"

// Optional progress callback to track batch execution cleanly
static void progress_callback(const PdfEngineReportResult* result,
                              void*                        user_ctx) {
    (void)user_ctx;
    if (result) {
        printf("[Progress] Report ID: %s | Status: %s\n",
               result->report_id ? result->report_id : "N/A",
               result->status == PDF_ENGINE_SUCCESS ? "SUCCESS" : "FAILED");
    }
}

int main(void) {
    const char* json_path = "data/mock_tax_report_5.json";
    const char* cert_path = "data/test_cert.pfx";
    const char* password  = "secret123";

    // Ensure output directory exists
    struct stat st = {0};
    if (stat(OUT_DIR, &st) == -1) {
#if defined(_WIN32)
        mkdir(OUT_DIR);
#else
        mkdir(OUT_DIR, 0755);
#endif
    }

    printf("Starting batch PDF generation and signing from: %s\n", json_path);

    int res = pdf_engine_generate_and_sign(
        json_path, PDF_REPORT_TYPE_TAX_REPORT, OUT_DIR, cert_path, password,
        progress_callback, NULL);

    if (res < 0) {
        fprintf(stderr, "Batch generation failed with error code: %d\n", res);
        return 1;
    }

    printf("Batch generation completed successfully! Check '%s/' directory.\n",
           OUT_DIR);
    return 0;
}
