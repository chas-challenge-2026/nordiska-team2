#include "pdf_gen/generator.h"

#include <stdio.h>

int main(int argc, char** argv) {
    (void)argc;
    (void)argv;

    printf("PDF Generator v0.1.0n");

    if (!pdf_gen_init()) {
        fprintf(stderr, "Failed to initialize PDF engine.\n");
        return 1;
    }

    PdfDocConfigT config = {.title  = "Monthly Bank Statement",
                               .author = "PDF Engine"};

    if (!pdf_gen_create_document("output.pdf", &config)) {
        fprintf(stderr, "Document creation failed.\n");
        return 1;
    }

    return 0;
}
