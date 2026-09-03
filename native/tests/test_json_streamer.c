#include "json_streamer/json_streamer.h"
#include "unity.h"

#include <stdio.h>
#include <string.h>

#define TEMP_TEST_FILE "temp_test_array.json"

// Test context to record callback invocations
typedef struct {
    int  call_count;
    char last_received[512];
    int  abort_on_count;
} TestContext;

void setUp(void) {
    // Runs before each test
}

void tearDown(void) {
    // Cleanup temporary files after each test
    remove(TEMP_TEST_FILE);
}

// Helper: Writes string content to a temporary test file
static void create_test_file(const char* content) {
    FILE* f = fopen(TEMP_TEST_FILE, "wb");
    TEST_ASSERT_NOT_NULL_MESSAGE(f, "Failed to create temp test file");
    fputs(content, f);
    fclose(f);
}

// Test Callback: Counts invocations and records the last string
static int mock_object_cb(const char* json_obj_str, void* user_ctx) {
    TestContext* ctx = (TestContext*)user_ctx;
    ctx->call_count++;

    strncpy(ctx->last_received, json_obj_str, sizeof(ctx->last_received) - 1);
    ctx->last_received[sizeof(ctx->last_received) - 1] = '\0';

    if (ctx->abort_on_count > 0 && ctx->call_count >= ctx->abort_on_count) {
        return -99; // Abort signal
    }

    return 0; // Continue streaming
}

// --- Test Cases ---

void test_streamer_should_extract_all_objects_from_array(void) {
    const char* json_data = "[\n"
                            "  { \"id\": 101, \"name\": \"Alpha\" },\n"
                            "  { \"id\": 102, \"name\": \"Beta\" },\n"
                            "  { \"id\": 103, \"name\": \"Gamma\" }\n"
                            "]";
    create_test_file(json_data);

    TestContext ctx = {0};
    int         status =
        json_stream_array_objects(TEMP_TEST_FILE, mock_object_cb, &ctx);

    TEST_ASSERT_EQUAL_INT(0, status);
    TEST_ASSERT_EQUAL_INT(3, ctx.call_count);

    // Updated assertion string to match raw whitespace in fixture
    TEST_ASSERT_EQUAL_STRING("{ \"id\": 103, \"name\": \"Gamma\" }",
                             ctx.last_received);
}

void test_streamer_should_handle_nested_objects_and_arrays(void) {
    const char* json_data = "[\n"
                            "  {\n"
                            "    \"taxpayer_id\": \"12345\",\n"
                            "    \"transactions\": [\n"
                            "      { \"amount\": 100.50 },\n"
                            "      { \"amount\": 250.00 }\n"
                            "    ]\n"
                            "  }\n"
                            "]";
    create_test_file(json_data);

    TestContext ctx = {0};
    int         status =
        json_stream_array_objects(TEMP_TEST_FILE, mock_object_cb, &ctx);

    TEST_ASSERT_EQUAL_INT(0, status);
    TEST_ASSERT_EQUAL_INT(1, ctx.call_count);
}

void test_streamer_should_handle_empty_array(void) {
    create_test_file("  [  \n \t ] ");

    TestContext ctx = {0};
    int         status =
        json_stream_array_objects(TEMP_TEST_FILE, mock_object_cb, &ctx);

    TEST_ASSERT_EQUAL_INT(0, status);
    TEST_ASSERT_EQUAL_INT(0, ctx.call_count);
}

void test_streamer_should_abort_when_callback_returns_error(void) {
    const char* json_data = "[\n"
                            "  { \"id\": 1 },\n"
                            "  { \"id\": 2 },\n"
                            "  { \"id\": 3 }\n"
                            "]";
    create_test_file(json_data);

    TestContext ctx = {.abort_on_count = 2}; // Abort on second item
    int         status =
        json_stream_array_objects(TEMP_TEST_FILE, mock_object_cb, &ctx);

    TEST_ASSERT_EQUAL_INT(-99, status); // Returns callback error code
    TEST_ASSERT_EQUAL_INT(2, ctx.call_count);
}

void test_streamer_should_return_error_for_nonexistent_file(void) {
    TestContext ctx    = {0};
    int         status = json_stream_array_objects("non_existent_file.json",
                                                   mock_object_cb, &ctx);

    TEST_ASSERT_EQUAL_INT(-2, status); // File not found error code
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_streamer_should_extract_all_objects_from_array);
    RUN_TEST(test_streamer_should_handle_nested_objects_and_arrays);
    RUN_TEST(test_streamer_should_handle_empty_array);
    RUN_TEST(test_streamer_should_abort_when_callback_returns_error);
    RUN_TEST(test_streamer_should_return_error_for_nonexistent_file);
    return UNITY_END();
}
