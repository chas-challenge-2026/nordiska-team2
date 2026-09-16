#pragma once

#ifdef __cplusplus
extern "C" {
#endif

// Opaque signer context holding the loaded key/cert
typedef struct PdfSigner PdfSigner;

/**
 * @brief Initializes a signer context by loading and unlocking a PFX file.
 */
PdfSigner* pdf_signer_create(const char* pfx_path, const char* password);

/**
 * @brief Cryptographically signs an existing PDF file using the loaded context.
 */
int pdf_signer_sign(PdfSigner* signer, const char* in_path,
                    const char* out_path);

/**
 * @brief Frees the signer context and its underlying OpenSSL keys/certs.
 */
void pdf_signer_free(PdfSigner* signer);

#ifdef __cplusplus
}
#endif
