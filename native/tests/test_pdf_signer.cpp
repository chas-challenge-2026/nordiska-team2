#include "signer/pdf_signer.h"
#include "unity.h"

#include <cstdio>
#include <cstdlib>
#include <podofo/podofo.h>
#include <string>

#define TEST_PFX_PATH "test_sig_temp.pfx"
#define TEST_PDF_PATH "test_input_temp.pdf"
#define TEST_OUT_PATH "test_output_temp.pdf"
#define TEST_PASSWORD "secret123"

extern "C" {
void setUp(void) {
    // Generate temporary PFX via OpenSSL
    std::string cmd =
        "openssl req -x509 -newkey rsa:2048 -keyout /tmp/k.pem -out /tmp/c.pem "
        "-days 1 -nodes -subj \"/CN=TestSigner\" 2>/dev/null && "
        "openssl pkcs12 -export -out " +
        std::string(TEST_PFX_PATH) +
        " -inkey /tmp/k.pem -in /tmp/c.pem -passout pass:" + TEST_PASSWORD +
        " 2>/dev/null && "
        "rm -f /tmp/k.pem /tmp/c.pem";
    int sys_ret = std::system(cmd.c_str());
    (void)sys_ret;

    // Programmatically generate a bulletproof valid PDF using PoDoFo
    try {
        PoDoFo::PdfMemDocument doc;
        doc.GetPages().CreatePage(PoDoFo::Rect(0, 0, 612, 792));
        doc.Save(TEST_PDF_PATH);
    } catch (...) {
        // Fallback or leave unhandled so test fails explicitly if setup breaks
    }
}

void tearDown(void) {
    std::remove(TEST_PFX_PATH);
    std::remove(TEST_PDF_PATH);
    std::remove(TEST_OUT_PATH);
}
}

void test_pdf_signer_create_null_or_empty_path(void) {
    TEST_ASSERT_NULL(pdf_signer_create(nullptr, TEST_PASSWORD));
    TEST_ASSERT_NULL(pdf_signer_create("", TEST_PASSWORD));
}

void test_pdf_signer_create_nonexistent_file(void) {
    TEST_ASSERT_NULL(
        pdf_signer_create("nonexistent_file_9999.pfx", TEST_PASSWORD));
}

void test_pdf_signer_create_wrong_password(void) {
    PdfSigner *signer = pdf_signer_create(TEST_PFX_PATH, "wrongpassword");
    TEST_ASSERT_NULL(signer);
}

void test_pdf_signer_create_success(void) {
    PdfSigner *signer = pdf_signer_create(TEST_PFX_PATH, TEST_PASSWORD);
    TEST_ASSERT_NOT_NULL(signer);
    if (signer) {
        pdf_signer_free(signer);
    }
}

void test_pdf_signer_sign_invalid_arguments(void) {
    PdfSigner *signer = pdf_signer_create(TEST_PFX_PATH, TEST_PASSWORD);

    TEST_ASSERT_EQUAL_INT(
        -1, pdf_signer_sign(nullptr, TEST_PDF_PATH, TEST_OUT_PATH));
    TEST_ASSERT_EQUAL_INT(-1, pdf_signer_sign(signer, nullptr, TEST_OUT_PATH));
    TEST_ASSERT_EQUAL_INT(-1, pdf_signer_sign(signer, TEST_PDF_PATH, nullptr));
    TEST_ASSERT_EQUAL_INT(-1, pdf_signer_sign(signer, "", TEST_OUT_PATH));

    if (signer) {
        pdf_signer_free(signer);
    }
}

void test_pdf_signer_sign_success(void) {
    PdfSigner *signer = pdf_signer_create(TEST_PFX_PATH, TEST_PASSWORD);
    TEST_ASSERT_NOT_NULL(signer);

    int result = pdf_signer_sign(signer, TEST_PDF_PATH, TEST_OUT_PATH);
    TEST_ASSERT_EQUAL_INT(0, result);

    FILE *out = std::fopen(TEST_OUT_PATH, "rb");
    TEST_ASSERT_NOT_NULL(out);
    if (out) {
        std::fseek(out, 0, SEEK_END);
        long size = std::ftell(out);
        std::fclose(out);
        TEST_ASSERT_GREATER_THAN(0, size);
    }

    pdf_signer_free(signer);
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_pdf_signer_create_null_or_empty_path);
    RUN_TEST(test_pdf_signer_create_nonexistent_file);
    RUN_TEST(test_pdf_signer_create_wrong_password);
    RUN_TEST(test_pdf_signer_create_success);
    RUN_TEST(test_pdf_signer_sign_invalid_arguments);
    RUN_TEST(test_pdf_signer_sign_success);
    return UNITY_END();
}
