#pragma once

#ifdef _WIN32
#    define INTEROP_API __declspec(dllexport)
#else
#    define INTEROP_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Generates and cryptographically signs a tax report PDF from JSON data.
 *
 * This function serves as the high-level entrypoint for the .NET interop layer.
 * It parses the provided JSON payload, builds the tax report layout using
 * libHaru, optionally signs the resulting PDF using OpenSSL and a PKCS#12
 * (.pfx) certificate, and writes the final output to disk.
 *
 * @param json_data A null-terminated UTF-8 string containing the raw JSON
 * payload with tax report details (e.g., tax year, user name, income,
 * deductions).
 * @param out_path  A null-terminated string specifying the local filesystem
 * path where the generated PDF file should be saved.
 * @param pfx_path  Optional. A null-terminated path to a .pfx/.p12 certificate
 * file used for signing. Pass NULL if digital signing is not required.
 * @param password  Optional. A null-terminated password string to unlock the
 * PFX certificate. Pass NULL if the certificate is unencrypted or if pfx_path
 * is NULL.
 *
 * @return int Returns `0` (zero) on success, or a non-zero error code on
 * failure (e.g., JSON parsing failure, file I/O error, or crypto/signing
 * error).
 */
INTEROP_API int pdf_generate_and_sign_tax_report(const char* json_data,
                                                 const char* out_path,
                                                 const char* pfx_path,
                                                 const char* password);

#ifdef __cplusplus
}
#endif
