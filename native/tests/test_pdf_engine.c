#include "pdf_engine.h"
#include "unity.h"

#include <stdio.h>
#include <unistd.h>

void setUp(void) {}
void tearDown(void) {}

void test_pdf_generate_and_sign_tax_report_mock(void) {
    const char* test_output = "test_tax_report.pdf";
    const char* json_path   = "{\n"
                              "  \"tax_year\": 2025,\n"
                              "  \"user_name\": \"Unity Test User\",\n"
                              "  \"total_income\": 300000.00,\n"
                              "  \"total_deductions\": 10000.00\n"
                              "}";

    int result =
        pdf_engine_generate_and_sign(json_path, 0, test_output, NULL, NULL);

    // Assert that generation succeeded (returns 0)
    TEST_ASSERT_EQUAL_INT(0, result);

    // Assert that the file was actually created and is non-empty
    FILE* f = fopen(test_output, "r");
    TEST_ASSERT_NOT_NULL(f);

    if (f) {
        fseek(f, 0, SEEK_END);
        long size = ftell(f);
        fclose(f);
        TEST_ASSERT_GREATER_THAN(0, size);
    }

    // Clean up test artifact
    unlink(test_output);
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_pdf_generate_and_sign_tax_report_mock);
    return UNITY_END();
}
