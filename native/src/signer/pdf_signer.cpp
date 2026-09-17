#include "pdf_signer.h"

#include "logging/log.h"

#include <openssl/err.h>
#include <openssl/evp.h>
#include <openssl/pkcs12.h>
#include <openssl/x509.h>

#include <podofo/podofo.h>

#include <cstdio>
#include <cstring>
#include <memory>
#include <vector>

struct PdfSigner {
    std::vector<unsigned char> cert_der; /**< leaf certificate, DER */
    std::vector<unsigned char> key_der;  /**< private key, DER (PKCS#8) */
    std::vector<std::vector<unsigned char>>
        chain_der; /**< intermediate certs, DER */
};

namespace {

void log_openssl_errors(const char *context) {
    unsigned long err;
    while ((err = ERR_get_error()) != 0) {
        char buf[256];
        ERR_error_string_n(err, buf, sizeof(buf));
        LOG_ERROR("%s: %s", context, buf);
    }
}

bool der_encode_cert(X509 *cert, std::vector<unsigned char> *out) {
    int len = i2d_X509(cert, nullptr);
    if (len <= 0) {
        return false;
    }
    out->resize((size_t)len);
    unsigned char *p = out->data();
    i2d_X509(cert, &p);
    return true;
}

bool der_encode_key(EVP_PKEY *pkey, std::vector<unsigned char> *out) {
    int len = i2d_PrivateKey(pkey, nullptr);
    if (len <= 0) {
        return false;
    }
    out->resize((size_t)len);
    unsigned char *p = out->data();
    i2d_PrivateKey(pkey, &p);
    return true;
}

bool copy_file(const char *src_path, const char *dst_path) {
    FILE *src = fopen(src_path, "rb");
    if (!src) {
        return false;
    }
    FILE *dst = fopen(dst_path, "wb");
    if (!dst) {
        fclose(src);
        return false;
    }
    char buf[65536];
    size_t n;
    bool ok = true;
    while ((n = fread(buf, 1, sizeof(buf), src)) > 0) {
        if (fwrite(buf, 1, n, dst) != n) {
            ok = false;
            break;
        }
    }
    ok = ok && (ferror(src) == 0);
    fclose(src);
    if (fclose(dst) != 0) {
        ok = false;
    }
    return ok;
}

} // namespace

PdfSigner *pdf_signer_create(const char *pfx_path, const char *password) {
    if (!pfx_path || !*pfx_path) {
        LOG_ERROR("pdf_signer_create called with no PFX path");
        return nullptr;
    }

    FILE *f = fopen(pfx_path, "rb");
    if (!f) {
        LOG_ERROR("Could not open PFX file '%s'", pfx_path);
        return nullptr;
    }
    PKCS12 *p12 = d2i_PKCS12_fp(f, nullptr);
    fclose(f);
    if (!p12) {
        LOG_ERROR("'%s' is not a valid PKCS#12 file", pfx_path);
        log_openssl_errors("PKCS12 parse");
        return nullptr;
    }

    EVP_PKEY *pkey = nullptr;
    X509 *cert = nullptr;
    STACK_OF(X509) *chain = nullptr;

    /* PKCS12_parse treats a NULL password as "no password was supplied",
     * which OpenSSL does not always treat the same as an empty-string
     * password used by some tools for unencrypted PFX files - normalize
     * to "" so both cases behave identically. */
    int parsed =
        PKCS12_parse(p12, password ? password : "", &pkey, &cert, &chain);
    PKCS12_free(p12);

    if (!parsed) {
        LOG_ERROR("Failed to unlock '%s' - wrong password or corrupt file",
                  pfx_path);
        log_openssl_errors("PKCS12 parse");
        return nullptr;
    }
    if (!pkey || !cert) {
        LOG_ERROR("'%s' did not contain both a private key and a certificate",
                  pfx_path);
        EVP_PKEY_free(pkey);
        X509_free(cert);
        sk_X509_pop_free(chain, X509_free);
        return nullptr;
    }
    if (!X509_check_private_key(cert, pkey)) {
        LOG_ERROR("The certificate and private key in '%s' do not match",
                  pfx_path);
        EVP_PKEY_free(pkey);
        X509_free(cert);
        sk_X509_pop_free(chain, X509_free);
        return nullptr;
    }

    std::unique_ptr<PdfSigner> signer(new PdfSigner());
    bool ok = der_encode_cert(cert, &signer->cert_der) &&
              der_encode_key(pkey, &signer->key_der);
    if (ok && chain) {
        for (int i = 0; i < sk_X509_num(chain); i++) {
            std::vector<unsigned char> der;
            if (!der_encode_cert(sk_X509_value(chain, i), &der)) {
                ok = false;
                break;
            }
            signer->chain_der.push_back(std::move(der));
        }
    }

    EVP_PKEY_free(pkey);
    X509_free(cert);
    sk_X509_pop_free(chain, X509_free);

    if (!ok) {
        LOG_ERROR("Failed to DER-encode the certificate or key from '%s'",
                  pfx_path);
        return nullptr;
    }
    return signer.release();
}

int pdf_signer_sign(PdfSigner *signer, const char *in_path,
                    const char *out_path) {
    if (!signer || !in_path || !*in_path || !out_path || !*out_path) {
        LOG_ERROR("pdf_signer_sign called with invalid arguments");
        return -1;
    }

    const bool SAME_PATH = (strcmp(in_path, out_path) == 0);
    const char *work_path = in_path;

    /* PoDoFo signs a document in place, as an incremental update, onto
     * the same device it was loaded from. To also support a different
     * out_path, copy the source there first and sign the copy in place;
     * that leaves in_path itself untouched either way, matching what a
     * caller would expect from separate in/out paths. */
    if (!SAME_PATH) {
        if (!copy_file(in_path, out_path)) {
            LOG_ERROR("Could not copy '%s' to '%s' before signing", in_path,
                      out_path);
            return -1;
        }
        work_path = out_path;
    }

    try {
        auto device = std::make_shared<PoDoFo::FileStreamDevice>(
            work_path, PoDoFo::FileMode::Open);
        PoDoFo::PdfMemDocument doc;
        doc.Load(device);

        auto &page = doc.GetPages().GetPageAt(0);
        auto &field = page.CreateField<PoDoFo::PdfSignature>("Signature1",
                                                             PoDoFo::Rect());

        PoDoFo::bufferview cert_view(
            reinterpret_cast<const char *>(signer->cert_der.data()),
            signer->cert_der.size());
        PoDoFo::bufferview key_view(
            reinterpret_cast<const char *>(signer->key_der.data()),
            signer->key_der.size());
        PoDoFo::PdfSignerCms cms_signer(cert_view, key_view);

        PoDoFo::SignDocument(doc, *device, cms_signer, field);
    } catch (const std::exception &e) {
        LOG_ERROR("Signing failed for '%s': %s", work_path, e.what());
        return -1;
    }

    return 0;
}

void pdf_signer_free(PdfSigner *signer) { delete signer; }
