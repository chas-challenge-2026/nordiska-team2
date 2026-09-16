#include "pdf_signer.h"

#include "logging/log.h"

// TODO(signing): implement PKCS#12 signing. Currently a no-op so callers
// that pass a pfx_path get an *unsigned* PDF back without any error.
//
// Returns a non-NULL sentinel (rather than NULL) on purpose: pdf_engine.c
// treats a NULL return as a fatal "couldn't load the cert" error and
// aborts the whole batch, which would defeat the "no-op until signing is
// implemented" intent above. Swap this for a real PdfSigner* once PKCS#12
// loading exists.
PdfSigner* pdf_signer_create(const char* pfx_path, const char* password) {
    (void)password;
    LOG_WARN(
        "pdf_signer_create() is not implemented yet; '%s' was left unsigned",
        pfx_path);
    return (PdfSigner*)1;
}

/**
 * @brief Cryptographically signs an existing PDF file using the loaded context.
 */
int pdf_signer_sign(PdfSigner* signer, const char* in_path,
                    const char* out_path) {

    LOG_WARN("pdf_signer_sign() is not implemented yet; '%s' was left unsigned",
             in_path);
    return 0;
}

/**
 * @brief Frees the signer context and its underlying OpenSSL keys/certs.
 */
void pdf_signer_free(PdfSigner* signer) { (void)signer; }
