#pragma once

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Digitally signs an existing PDF file using a PKCS#12 / PFX
 * certificate.
 *
 * @param input_pdf_path Path to the unsigned PDF file.
 * @param output_signed_path Path where the signed PDF file will be written.
 * @param pfx_path Path to the .pfx/.p12 signing certificate file.
 * @param password Password for the .pfx certificate.
 * @return int Returns 0 on success, or a non-zero error code on failure.
 */
int pdf_signer_sign(const char* input_pdf_path, const char* output_signed_path,
                    const char* pfx_path, const char* password);

#ifdef __cplusplus
}
#endif
