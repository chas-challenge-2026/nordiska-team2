#include "interop_api.h"

#include <stdio.h>
#include <stdlib.h>
#include <sys/stat.h>
#include <sys/types.h>
// TODO:implement arg parser and use pdg engine
int main(int argc, char** argv) {
    (void)argc;
    (void)argv;

    // THIS IS JUST MOCK
    struct stat st = {0};
    if (stat("data", &st) == -1) {
        mkdir("data", 0700);
    }

    const char* out_path = "data/tax_report.pdf";

    const char* mock_json = "{\n"
                            "  \"tax_year\": 2025,\n"
                            "  \"user_name\": \"Test User\",\n"
                            "  \"total_income\": 450000.00,\n"
                            "  \"total_deductions\": 25000.00\n"
                            "}";

    printf("Generating mock tax report to %s...\n", out_path);

    int result =
        pdf_generate_and_sign_tax_report(mock_json, out_path, NULL, NULL);

    if (result == 0) {
        printf("Successfully generated tax report PDF at: %s\n", out_path);
        return 0;
    } else {
        fprintf(stderr, "Failed to generate PDF, error code: %d\n", result);
        return 1;
    }
}
