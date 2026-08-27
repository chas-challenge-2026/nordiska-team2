#include "interop_api.h"

#include "generator/pdf_generator.h"
#include "layouts/tax_report.h"
#include "signer/pdf_signer.h"

// TODO: implement this for real
int pdf_generate_and_sign_tax_report(const char* json_data,
                                     const char* out_path, const char* pfx_path,
                                     const char* password) {

    int gen_result =
        pdf_generator_generate(json_data, out_path, tax_report_layout);
    if (gen_result != 0) {
        return gen_result;
    }

    // sign PDFS here

    return 0;
}
