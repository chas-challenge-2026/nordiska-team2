#include "pdf_engine.h"

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

    const char* json_path = "report.json";
    printf("Generating mock tax report to %s...\n", out_path);

    int result =
        pdf_engine_generate_and_sign(json_path, 0, out_path, NULL, NULL);

    if (result == 0) {
        printf("Successfully generated tax report PDF at: %s\n", out_path);
        return 0;
    } else {
        fprintf(stderr, "Failed to generate PDF, error code: %d\n", result);
        return 1;
    }
}
