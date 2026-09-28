#define ANKERL_NANOBENCH_IMPLEMENTATION
#include "nanobench.h"
#include "pdf_engine.h"

#include <chrono>
#include <cstdlib>
#include <filesystem>
#include <iostream>
#include <stdio.h>
#include <string>

#define OUT_DIR "data/output_pdfs"
#define MAX_ALLOWED_SECONDS 12.0

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
        std::cerr << "Error: Failed to find 1k mock JSON dataset.\n";
        return 1;
    }

    auto start_time = std::chrono::steady_clock::now();

    ankerl::nanobench::Bench bench;
    bench.warmup(0).epochs(1).minEpochIterations(1).run(
        "Generate and Sign 1k PDFs", [&]() {
            int res = pdf_engine_generate_and_sign(
                json_path.c_str(), PDF_REPORT_TYPE_TAX_REPORT, OUT_DIR,
                cert_path.c_str(), "secret123", silent_progress, NULL);

            if (res < 0) {
                std::cerr << "Batch generation failed with error code: " << res
                          << "\n";
            }
        });

    auto end_time = std::chrono::steady_clock::now();
    std::chrono::duration<double> elapsed = end_time - start_time;

    std::cout << "BENCH_ELAPSED_SEC=" << elapsed.count() << "\n";

    if (elapsed.count() > MAX_ALLOWED_SECONDS) {
        std::cerr << "Benchmark failed: Took " << elapsed.count()
                  << "s (Limit: " << MAX_ALLOWED_SECONDS << "s)\n";
        return 1;
    }

    return 0;
}
