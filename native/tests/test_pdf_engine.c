#include "pdf_engine.h"
#include "unity.h"

#include <dirent.h>
#include <stdio.h>
#include <string.h>
#include <sys/stat.h>
#include <unistd.h>

#define TEMP_JSON_FILE "temp_test_batch.json"
#define TEMP_OUT_DIR "temp_test_out"

void setUp(void) {}

static void remove_dir_contents(const char* dir) {
    DIR* d = opendir(dir);
    if (!d) {
        return;
    }
    struct dirent* entry;
    char           path[512];
    while ((entry = readdir(d)) != NULL) {
        if (strcmp(entry->d_name, ".") == 0 ||
            strcmp(entry->d_name, "..") == 0) {
            continue;
        }
        snprintf(path, sizeof(path), "%s/%s", dir, entry->d_name);
        remove(path); // works for plain files; leftover subdirs are handled
                      // per-test
    }
    closedir(d);
}

void tearDown(void) {
    remove(TEMP_JSON_FILE);
    remove_dir_contents(TEMP_OUT_DIR);
    remove(TEMP_OUT_DIR);
}

static void write_json_fixture(const char* content) {
    FILE* f = fopen(TEMP_JSON_FILE, "wb");
    TEST_ASSERT_NOT_NULL_MESSAGE(f, "Failed to create temp JSON fixture");
    fputs(content, f);
    fclose(f);
}

static int pdf_file_looks_valid(const char* path) {
    FILE* f = fopen(path, "rb");
    if (!f) {
        return 0;
    }
    char   header[5]  = {0};
    size_t read_count = fread(header, 1, 4, f);
    fclose(f);
    return read_count == 4 && strncmp(header, "%PDF", 4) == 0;
}

// Matches the schema tax_report_layout() actually reads, so a successful
// run proves the real pipeline works, not just that libHaru can write a
// blank page. Writes into a caller-supplied buffer rather than returning a
// static one, since tests need to build two of these in a single fixture -
// a shared static buffer would get overwritten before both calls are read.
static void build_report_json(const char* account_number, char* out,
                              size_t out_cap) {
    snprintf(
        out, out_cap,
        "{\n"
        "  \"metadata\": {\"report_id\": \"TAX-UNITY-1\", \"year\": 2026,\n"
        "                \"period_start\": \"2026-01-01\", \"period_end\": "
        "\"2026-12-31\"},\n"
        "  \"customer\": {\"full_name\": \"Unity Test User\", \"personal_id\": "
        "\"000101-0000\",\n"
        "                \"address\": \"Testgatan 1\"},\n"
        "  \"account\": {\"account_number\": \"%s\", \"account_type\": "
        "\"Testkonto\",\n"
        "               \"interest_rate_pct\": 1.0},\n"
        "  \"summary\": {\"starting_balance_sek\": 100.0, "
        "\"ending_balance_sek\": 110.0,\n"
        "               \"total_deposits_sek\": 10.0, "
        "\"total_withdrawals_sek\": 0.0,\n"
        "               \"total_interest_earned_sek\": 10.0, "
        "\"total_tax_withheld_sek\": 0.0},\n"
        "  \"transactions\": []\n"
        "}",
        account_number);
}

typedef struct {
    int  call_count;
    int  fail_count;
    int  last_status;
    char last_report_id[96];
} ProgressLog;

static void record_progress(const PdfEngineReportResult* result,
                            void*                        user_ctx) {
    ProgressLog* log = (ProgressLog*)user_ctx;
    log->call_count++;
    log->last_status = result->status;
    if (result->status != PDF_ENGINE_SUCCESS) {
        log->fail_count++;
    }
    strncpy(log->last_report_id, result->report_id,
            sizeof(log->last_report_id) - 1);
    log->last_report_id[sizeof(log->last_report_id) - 1] = '\0';
}

void test_batch_generates_one_pdf_per_report_named_by_account_number(void) {
    char report1[1024];
    char report2[1024];
    build_report_json("ACC-1", report1, sizeof(report1));
    build_report_json("ACC-2", report2, sizeof(report2));

    char fixture[2200];
    snprintf(fixture, sizeof(fixture), "[\n%s,\n%s\n]", report1, report2);
    write_json_fixture(fixture);

    ProgressLog log    = {0};
    int         result = pdf_engine_generate_and_sign(
        TEMP_JSON_FILE, PDF_REPORT_TYPE_TAX_REPORT, TEMP_OUT_DIR, NULL, NULL,
        record_progress, &log);

    TEST_ASSERT_EQUAL_INT(2, result);
    TEST_ASSERT_EQUAL_INT(2, log.call_count);
    TEST_ASSERT_EQUAL_INT(0, log.fail_count);
    TEST_ASSERT_TRUE(pdf_file_looks_valid(TEMP_OUT_DIR "/ACC-1.pdf"));
    TEST_ASSERT_TRUE(pdf_file_looks_valid(TEMP_OUT_DIR "/ACC-2.pdf"));
}

void test_batch_sanitizes_unsafe_account_numbers_and_stays_inside_out_dir(
    void) {
    char report[1024];
    build_report_json("../../etc/evil", report, sizeof(report));

    char fixture[1100];
    snprintf(fixture, sizeof(fixture), "[\n%s\n]", report);
    write_json_fixture(fixture);

    ProgressLog log    = {0};
    int         result = pdf_engine_generate_and_sign(
        TEMP_JSON_FILE, PDF_REPORT_TYPE_TAX_REPORT, TEMP_OUT_DIR, NULL, NULL,
        record_progress, &log);

    TEST_ASSERT_EQUAL_INT(1, result);
    TEST_ASSERT_NULL_MESSAGE(strchr(log.last_report_id, '/'),
                             "Sanitized report id must not contain '/'");
    char expected_path[256];
    snprintf(expected_path, sizeof(expected_path), "%s/%s.pdf", TEMP_OUT_DIR,
             log.last_report_id);
    TEST_ASSERT_TRUE(pdf_file_looks_valid(expected_path));
}

// Forces one report to fail without touching its JSON content: a directory
// is pre-created where that report's output PDF would be written, so
// libHaru's HPDF_SaveToFile() fails for that one report only. Proves a
// single failure doesn't abort the rest of the batch.
void test_batch_continues_after_one_report_fails(void) {
    mkdir(TEMP_OUT_DIR, 0755);
    char blocked_path[256];
    snprintf(blocked_path, sizeof(blocked_path), "%s/ACC-BLOCKED.pdf",
             TEMP_OUT_DIR);
    TEST_ASSERT_EQUAL_INT_MESSAGE(0, mkdir(blocked_path, 0755),
                                  "Failed to pre-create blocking directory");

    char report1[1024];
    char report2[1024];
    build_report_json("ACC-BLOCKED", report1, sizeof(report1));
    build_report_json("ACC-OK", report2, sizeof(report2));

    char fixture[2200];
    snprintf(fixture, sizeof(fixture), "[\n%s,\n%s\n]", report1, report2);
    write_json_fixture(fixture);

    ProgressLog log    = {0};
    int         result = pdf_engine_generate_and_sign(
        TEMP_JSON_FILE, PDF_REPORT_TYPE_TAX_REPORT, TEMP_OUT_DIR, NULL, NULL,
        record_progress, &log);

    TEST_ASSERT_EQUAL_INT(1, result);         // one of the two succeeded
    TEST_ASSERT_EQUAL_INT(2, log.call_count); // both were still attempted
    TEST_ASSERT_EQUAL_INT(1, log.fail_count);
    TEST_ASSERT_TRUE(pdf_file_looks_valid(TEMP_OUT_DIR "/ACC-OK.pdf"));

    rmdir(blocked_path);
}

void test_batch_works_without_a_progress_callback(void) {
    // A bare object (no array wrapper) is also accepted - see
    // json_streamer.c, which only strips '[', ']', ',' at depth 0 if
    // present, rather than requiring them.
    char report[1024];
    build_report_json("ACC-NOCB", report, sizeof(report));
    write_json_fixture(report);

    int result =
        pdf_engine_generate_and_sign(TEMP_JSON_FILE, PDF_REPORT_TYPE_TAX_REPORT,
                                     TEMP_OUT_DIR, NULL, NULL, NULL, NULL);

    TEST_ASSERT_EQUAL_INT(1, result);
}

void test_batch_rejects_null_json_path(void) {
    int result = pdf_engine_generate_and_sign(
        NULL, PDF_REPORT_TYPE_TAX_REPORT, TEMP_OUT_DIR, NULL, NULL, NULL, NULL);

    TEST_ASSERT_EQUAL_INT(PDF_ENGINE_ERROR_INVALID_ARGS, result);
}

void test_batch_rejects_empty_out_dir(void) {
    char report[1024];
    build_report_json("ACC-1", report, sizeof(report));
    write_json_fixture(report);

    int result = pdf_engine_generate_and_sign(
        TEMP_JSON_FILE, PDF_REPORT_TYPE_TAX_REPORT, "", NULL, NULL, NULL, NULL);

    TEST_ASSERT_EQUAL_INT(PDF_ENGINE_ERROR_INVALID_ARGS, result);
}

void test_batch_reports_missing_input_file(void) {
    int result = pdf_engine_generate_and_sign(
        "no_such_file.json", PDF_REPORT_TYPE_TAX_REPORT, TEMP_OUT_DIR, NULL,
        NULL, NULL, NULL);

    TEST_ASSERT_EQUAL_INT(PDF_ENGINE_ERROR_JSON_READ_FAILED, result);
}

void test_batch_reports_empty_array(void) {
    write_json_fixture("[]");

    int result =
        pdf_engine_generate_and_sign(TEMP_JSON_FILE, PDF_REPORT_TYPE_TAX_REPORT,
                                     TEMP_OUT_DIR, NULL, NULL, NULL, NULL);

    TEST_ASSERT_EQUAL_INT(PDF_ENGINE_ERROR_NO_REPORT_OBJECT, result);
}

void test_batch_reports_unknown_report_type(void) {
    char report[1024];
    build_report_json("ACC-1", report, sizeof(report));
    write_json_fixture(report);

    // Deliberately out of PdfReportType's declared range, to exercise the
    // "unknown layout" error path.
    int result = pdf_engine_generate_and_sign(
        TEMP_JSON_FILE,
        // NOLINTNEXTLINE(clang-analyzer-optin.core.EnumCastOutOfRange)
        (PdfReportType)999, TEMP_OUT_DIR, NULL, NULL, NULL, NULL);

    TEST_ASSERT_EQUAL_INT(PDF_ENGINE_ERROR_UNKNOWN_REPORT_TYPE, result);
}

void test_batch_creates_out_dir_if_missing(void) {
    char report[1024];
    build_report_json("ACC-NEWDIR", report, sizeof(report));
    write_json_fixture(report);

    int result =
        pdf_engine_generate_and_sign(TEMP_JSON_FILE, PDF_REPORT_TYPE_TAX_REPORT,
                                     TEMP_OUT_DIR, NULL, NULL, NULL, NULL);

    TEST_ASSERT_EQUAL_INT(1, result);
    TEST_ASSERT_TRUE(pdf_file_looks_valid(TEMP_OUT_DIR "/ACC-NEWDIR.pdf"));
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_batch_generates_one_pdf_per_report_named_by_account_number);
    RUN_TEST(
        test_batch_sanitizes_unsafe_account_numbers_and_stays_inside_out_dir);
    RUN_TEST(test_batch_continues_after_one_report_fails);
    RUN_TEST(test_batch_works_without_a_progress_callback);
    RUN_TEST(test_batch_rejects_null_json_path);
    RUN_TEST(test_batch_rejects_empty_out_dir);
    RUN_TEST(test_batch_reports_missing_input_file);
    RUN_TEST(test_batch_reports_empty_array);
    RUN_TEST(test_batch_reports_unknown_report_type);
    RUN_TEST(test_batch_creates_out_dir_if_missing);
    return UNITY_END();
}
