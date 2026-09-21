#define ANKERL_NANOBENCH_IMPLEMENTATION
#include "nanobench.h"
#include "pdf_engine.h"

#include <cstdlib>
#include <filesystem>
#include <stdio.h>
#include <string>

#define OUT_DIR "data/output_pdfs"

static void silent_progress(const PdfEngineReportResult *result,
                            void *user_ctx) {
    (void)result;
    (void)user_ctx;
}

int main() {
    std::string root_dir = PROJECT_ROOT_DIR;
    std::string json_path = root_dir + "/data/mock_tax_report_1k.json";
    std::string cert_path = root_dir + "/data/test_cert.pfx";

    std::filesystem::create_directories(root_dir + "/" + OUT_DIR);

    if (!std::filesystem::exists(json_path)) {
        printf("1k mock JSON missing. Generating via Python script...\n");
        std::string py_cmd =
            "python3 " + root_dir + "/scripts/generate_1k_mock.py";
        if (std::system(py_cmd.c_str()) != 0) {
            fprintf(stderr,
                    "Error: Failed to generate 1k mock JSON dataset.\n");
            return 1;
        }
    }

    ankerl::nanobench::Bench().warmup(0).epochs(1).minEpochIterations(1).run(
        "Generate and Sign 1k PDFs", [&]() {
            int res = pdf_engine_generate_and_sign(
                json_path.c_str(), PDF_REPORT_TYPE_TAX_REPORT, OUT_DIR,
                cert_path.c_str(), "secret123", silent_progress, NULL);

            if (res < 0) {
                fprintf(stderr, "Batch generation failed with error code: %d\n",
                        res);
            }
        });

    return 0;
}
