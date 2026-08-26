#include "pdf_gen/generator.h"
#include "unity.h"

void setUp(void) {}
void tearDown(void) {}

void test_pdf_gen_init(void) { TEST_ASSERT_TRUE(pdf_gen_init()); }

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_pdf_gen_init);
    return UNITY_END();
}
