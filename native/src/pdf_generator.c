#include "pdf_gen/generator.h"

#include <stdio.h>

bool pdf_gen_init(void) {
    // Pipeline / library initialization stub
    return true;
}

bool pdf_gen_create_document(const char*             output_path,
                             const PdfDocConfigT* config) {
    if (!output_path || !config) {
        return false;
    }
    printf("[PDF-GEN] Writing stub document to '%s' (Title: '%s')\n",
           output_path, config->title);
    return true;
}
