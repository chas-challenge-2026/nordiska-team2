#include "pdf_signer.h"

#include "logging/log.h"

// TODO(signing): implement PKCS#12 signing. Currently a no-op so callers
// that pass a pfx_path get an *unsigned* PDF back without any error.
int pdf_signer_sign(const char* input_pdf_path, const char* output_signed_path,
                    const char* pfx_path, const char* password) {
    (void)input_pdf_path;
    (void)output_signed_path;
    (void)password;
    LOG_WARN("pdf_signer_sign() is not implemented yet; '%s' was left unsigned",
             pfx_path);
    return 0;
}
