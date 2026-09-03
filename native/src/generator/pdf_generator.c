#include "pdf_generator.h"

#include <stdio.h>
#include <stdlib.h>

// Error handler callback for libHaru so it doesn't crash on failure
static void error_handler(HPDF_STATUS error_no, HPDF_STATUS detail_no,
                          void* user_data) {
    fprintf(stderr, "libHaru error: error_no=0x%04X, detail_no=%d\n",
            (unsigned int)error_no, (int)detail_no);
}

int pdf_generator_generate(const char* json_str, const char* output_path,
                           PdfLayoutFn layout_fn) {
    if (!output_path || !layout_fn) {
        return PDF_ERROR_INVALID_ARGS;
    }

    HPDF_Doc pdf = HPDF_New(error_handler, NULL);
    if (!pdf) {
        return PDF_ERROR_OUT_OF_MEMORY;
    }

    cJSON* root = NULL;
    if (json_str != NULL && json_str[0] != '\0') {
        root = cJSON_Parse(json_str);
        if (root == NULL) {
            const char* error_ptr = cJSON_GetErrorPtr();
            if (error_ptr != NULL) {
                fprintf(stderr, "cJSON Error before: %s\n", error_ptr);
            }
            HPDF_Free(pdf);
            return PDF_ERROR_JSON_PARSE;
        }
    }

    int layout_result = layout_fn(pdf, root);

    if (root != NULL) {
        cJSON_Delete(root);
    }

    if (layout_result != 0) {
        HPDF_Free(pdf);
        return PDF_ERROR_LAYOUT_FAILED;
    }

    if (HPDF_SaveToFile(pdf, output_path) != HPDF_OK) {
        HPDF_Free(pdf);
        return PDF_ERROR_SAVE_FAILED;
    }

    HPDF_Free(pdf);

    return PDF_SUCCESS;
}
