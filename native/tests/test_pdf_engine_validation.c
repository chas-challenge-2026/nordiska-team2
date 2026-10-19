#include "pdf_engine.h"
#include "unity.h"

#include <dirent.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <unistd.h>

#define TEMP_JSON_FILE "temp_val_batch.json"
#define TEMP_OUT_DIR "temp_val_out"

/* Fixture fragments */
#define T_META                                                                 \
    "\"metadata\":{\"report_id\":\"TAX-1\",\"year\":2026,"                     \
    "\"period_start\":\"2026-01-01\",\"period_end\":\"2026-12-31\"}"
#define T_CUST                                                                 \
    "\"customer\":{\"full_name\":\"Test User\","                               \
    "\"personal_id\":\"000101-0000\",\"address\":\"Testgatan 1\"}"
#define T_ACC(id)                                                              \
    "\"account\":{\"account_number\":\"" id                                    \
    "\",\"account_type\":\"Testkonto\",\"interest_rate_pct\":1.0}"
#define T_ACC_NO_RATE(id)                                                      \
    "\"account\":{\"account_number\":\"" id "\",\"account_type\":"             \
    "\"Testkonto\"}"
#define T_SUM                                                                  \
    "\"summary\":{\"starting_balance_sek\":100.0,"                             \
    "\"ending_balance_sek\":110.0,\"total_deposits_sek\":10.0,"                \
    "\"total_withdrawals_sek\":0.0,\"total_interest_earned_sek\":10.0,"        \
    "\"total_tax_withheld_sek\":0.0}"
#define TX_OK                                                                  \
    "{\"date\":\"2026-01-02\",\"type\":\"Deposit\","                           \
    "\"description\":\"Salary\",\"amount_sek\":10.0,\"balance_after_sek\":"    \
    "110.0}"
#define TXS(items) "\"transactions\":[" items "]"
#define TAX_BODY(acc, txs) T_META "," T_CUST "," acc "," T_SUM "," txs

#define B_META                                                                 \
    "\"metadata\":{\"statement_id\":\"STMT-1\","                               \
    "\"period_start\":\"2026-01-01\",\"period_end\":\"2026-01-31\"}"
#define B_SUM                                                                  \
    "\"summary\":{\"opening_balance_sek\":100.0,"                              \
    "\"closing_balance_sek\":110.0,\"total_deposits_sek\":10.0,"               \
    "\"total_withdrawals_sek\":0.0,\"transaction_count\":1}"

/* Harness */
#define MAX_LOG 16
typedef struct {
    int  call_count, fail_count, detail_was_null;
    int  status[MAX_LOG];
    char id[MAX_LOG][96];
    char detail[MAX_LOG][192];
} ProgressLog;

static void record_progress(const PdfEngineReportResult* r, void* ctx) {
    ProgressLog* log = (ProgressLog*)ctx;
    int          i   = log->call_count++;
    if (r->status != PDF_ENGINE_SUCCESS)
        log->fail_count++;
    if (!r->detail)
        log->detail_was_null = 1;
    if (i < MAX_LOG) {
        log->status[i] = r->status;
        snprintf(log->id[i], sizeof(log->id[i]), "%s", r->report_id);
        snprintf(log->detail[i], sizeof(log->detail[i]), "%s",
                 r->detail ? r->detail : "");
    }
}

void setUp(void) {}

static void remove_dir_contents(const char* dir) {
    DIR* d = opendir(dir);
    if (!d)
        return;
    struct dirent* e;
    char           path[512];
    while ((e = readdir(d)) != NULL) {
        if (!strcmp(e->d_name, ".") || !strcmp(e->d_name, ".."))
            continue;
        snprintf(path, sizeof(path), "%s/%s", dir, e->d_name);
        remove(path);
    }
    closedir(d);
}

void tearDown(void) {
    remove(TEMP_JSON_FILE);
    remove_dir_contents(TEMP_OUT_DIR);
    remove(TEMP_OUT_DIR);
}

static int run(PdfReportType type, const char* json, ProgressLog* log) {
    FILE* f = fopen(TEMP_JSON_FILE, "wb");
    TEST_ASSERT_NOT_NULL(f);
    fputs(json, f);
    fclose(f);
    return pdf_engine_generate_and_sign(TEMP_JSON_FILE, type, TEMP_OUT_DIR,
                                        NULL, NULL, record_progress, log);
}

static int file_exists(const char* p) {
    struct stat st;
    return stat(p, &st) == 0;
}

static int pdf_looks_valid(const char* path) {
    FILE* f = fopen(path, "rb");
    if (!f)
        return 0;
    char   h[5] = {0};
    size_t n    = fread(h, 1, 4, f);
    fclose(f);
    return n == 4 && strncmp(h, "%PDF", 4) == 0;
}

static int count_entries(const char* dir) {
    DIR* d = opendir(dir);
    if (!d)
        return 0;
    int            n = 0;
    struct dirent* e;
    while ((e = readdir(d)) != NULL)
        if (strcmp(e->d_name, ".") && strcmp(e->d_name, ".."))
            n++;
    closedir(d);
    return n;
}

static void assert_single_rejection(int result, const ProgressLog* log,
                                    const char* detail_substr) {
    TEST_ASSERT_EQUAL_INT(0, result);
    TEST_ASSERT_EQUAL_INT(1, log->call_count);
    TEST_ASSERT_EQUAL_INT(PDF_ENGINE_ERROR_MISSING_FIELD, log->status[0]);
    TEST_ASSERT_NOT_NULL_MESSAGE(strstr(log->detail[0], detail_substr),
                                 log->detail[0]);
    TEST_ASSERT_EQUAL_INT_MESSAGE(0, count_entries(TEMP_OUT_DIR),
                                  "rejected report must leave no files");
}

/* Happy paths */
void test_empty_transactions_array_is_valid(void) {
    ProgressLog log = {0};
    TEST_ASSERT_EQUAL_INT(1, run(PDF_REPORT_TYPE_TAX_REPORT,
                                 "[{" TAX_BODY(T_ACC("ACC-1"), TXS("")) "}]",
                                 &log));
    TEST_ASSERT_TRUE(pdf_looks_valid(TEMP_OUT_DIR "/ACC-1.pdf"));
}

void test_transaction_without_description_is_valid(void) {
    ProgressLog log = {0};
    TEST_ASSERT_EQUAL_INT(
        1,
        run(PDF_REPORT_TYPE_TAX_REPORT,
            "[{" TAX_BODY(
                T_ACC("ACC-1"),
                TXS("{\"date\":\"2026-01-02\",\"type\":\"Deposit\","
                    "\"amount_sek\":10.0,\"balance_after_sek\":110.0}")) "}]",
            &log));
}

void test_success_reports_non_null_empty_detail(void) {
    ProgressLog log = {0};
    run(PDF_REPORT_TYPE_TAX_REPORT,
        "[{" TAX_BODY(T_ACC("ACC-1"), TXS(TX_OK)) "}]", &log);
    TEST_ASSERT_FALSE(log.detail_was_null);
    TEST_ASSERT_EQUAL_STRING("", log.detail[0]);
}

void test_non_ascii_names_render_without_failing(void) {
    ProgressLog log = {0};
    TEST_ASSERT_EQUAL_INT(
        1, run(PDF_REPORT_TYPE_TAX_REPORT,
               "[{" T_META ",\"customer\":{\"full_name\":\"\xc3\x85sa \xc3\x96"
               "berg\","
               "\"personal_id\":\"1\",\"address\":\"G\xc3\xa5"
               "gatan 1\"}," T_ACC("ACC-SV") "," T_SUM "," TXS(TX_OK) "}]",
               &log));
}

void test_many_transactions_paginate(void) {
    const int N   = 300;
    size_t    cap = (size_t)N * 200 + 4096;
    char*     buf = malloc(cap);
    TEST_ASSERT_NOT_NULL(buf);
    size_t len =
        (size_t)snprintf(buf, cap,
                         "[{" T_META "," T_CUST
                         "," T_ACC("ACC-BIG") "," T_SUM ",\"transactions\":[");
    for (int i = 0; i < N; i++)
        len += (size_t)snprintf(buf + len, cap - len, "%s" TX_OK, i ? "," : "");
    snprintf(buf + len, cap - len, "]}]");
    ProgressLog log = {0};
    int         r   = run(PDF_REPORT_TYPE_TAX_REPORT, buf, &log);
    free(buf);
    TEST_ASSERT_EQUAL_INT(1, r);
    TEST_ASSERT_TRUE(pdf_looks_valid(TEMP_OUT_DIR "/ACC-BIG.pdf"));
}

/* Validation failures */
void test_missing_customer_name_is_rejected_with_field_name(void) {
    ProgressLog log = {0};
    int         r =
        run(PDF_REPORT_TYPE_TAX_REPORT,
            "[{" T_META
            ",\"customer\":{\"personal_id\":\"1\",\"address\":\"x\"}," T_ACC(
                "ACC-1") "," T_SUM "," TXS("") "}]",
            &log);
    assert_single_rejection(r, &log, "customer.full_name");
}

void test_wrong_type_is_rejected(void) {
    ProgressLog log = {0};
    int         r   = run(PDF_REPORT_TYPE_TAX_REPORT,
                          "[{\"metadata\":{\"report_id\":\"T\",\"year\":\"2026\","
                                    "\"period_start\":\"a\",\"period_end\":\"b\"}," T_CUST
                          "," T_ACC("ACC-1") "," T_SUM "," TXS("") "}]",
                          &log);
    assert_single_rejection(r, &log, "metadata.year");
}

void test_null_value_counts_as_missing(void) {
    ProgressLog log = {0};
    int         r   = run(PDF_REPORT_TYPE_TAX_REPORT,
                          "[{" T_META
                          ",\"customer\":{\"full_name\":null,\"personal_id\":\"1\","
                                    "\"address\":\"x\"}," T_ACC("ACC-1") "," T_SUM "," TXS("") "}]",
                          &log);
    assert_single_rejection(r, &log, "customer.full_name");
}

void test_empty_required_string_is_rejected(void) {
    ProgressLog log = {0};
    int         r   = run(PDF_REPORT_TYPE_TAX_REPORT,
                          "[{" TAX_BODY(T_ACC(""), TXS("")) "}]", &log);
    assert_single_rejection(r, &log, "account.account_number");
}

void test_missing_transactions_array_is_rejected(void) {
    ProgressLog log = {0};
    int         r   = run(PDF_REPORT_TYPE_TAX_REPORT,
                          "[{" T_META "," T_CUST "," T_ACC("ACC-1") "," T_SUM "}]", &log);
    assert_single_rejection(r, &log, "transactions");
}

void test_bad_transaction_row_names_index_and_field(void) {
    ProgressLog log = {0};
    int         r   = run(PDF_REPORT_TYPE_TAX_REPORT,
                          "[{" TAX_BODY(T_ACC("ACC-1"),
                                        TXS(TX_OK ",{\"date\":\"d\",\"type\":\"t\","
                                                            "\"balance_after_sek\":1.0}")) "}]",
                          &log);
    assert_single_rejection(r, &log, "transactions[1].amount_sek");
}

void test_empty_object_is_rejected(void) {
    ProgressLog log = {0};
    assert_single_rejection(run(PDF_REPORT_TYPE_TAX_REPORT, "[{}]", &log), &log,
                            "metadata");
}

/* Batch semantics */
void test_bad_report_in_batch_does_not_stop_the_others(void) {
    ProgressLog log = {0};
    int         r =
        run(PDF_REPORT_TYPE_TAX_REPORT,
            "[{" TAX_BODY(T_ACC("ACC-GOOD1"),
                          TXS(TX_OK)) "},"
                                      "{" T_META
                                      "," T_ACC("ACC-BAD") "," T_SUM "," TXS(
                                          "") "},"
                                              "{" TAX_BODY(T_ACC("ACC-GOOD2"),
                                                           TXS("")) "}]",
            &log);
    TEST_ASSERT_EQUAL_INT(2, r);
    TEST_ASSERT_EQUAL_INT(3, log.call_count);
    TEST_ASSERT_EQUAL_INT(PDF_ENGINE_ERROR_MISSING_FIELD, log.status[1]);
    TEST_ASSERT_TRUE(pdf_looks_valid(TEMP_OUT_DIR "/ACC-GOOD1.pdf"));
    TEST_ASSERT_TRUE(pdf_looks_valid(TEMP_OUT_DIR "/ACC-GOOD2.pdf"));
    TEST_ASSERT_FALSE(file_exists(TEMP_OUT_DIR "/ACC-BAD.pdf"));
}

void test_rejected_report_is_identifiable_by_position(void) {
    ProgressLog log = {0};
    run(PDF_REPORT_TYPE_TAX_REPORT,
        "[{" TAX_BODY(T_ACC("ACC-1"), TXS("")) "},{\"metadata\":{}}]", &log);
    TEST_ASSERT_EQUAL_STRING("report_1", log.id[1]);
}

void test_failed_publish_leaves_no_temp_file(void) {
    mkdir(TEMP_OUT_DIR, 0755);
    TEST_ASSERT_EQUAL_INT(0, mkdir(TEMP_OUT_DIR "/ACC-BLOCKED.pdf", 0755));
    ProgressLog log = {0};
    int         r   = run(PDF_REPORT_TYPE_TAX_REPORT,
                          "[{" TAX_BODY(T_ACC("ACC-BLOCKED"), TXS("")) "}]", &log);
    TEST_ASSERT_EQUAL_INT(0, r);
    TEST_ASSERT_EQUAL_INT(PDF_ENGINE_ERROR_GENERATION_FAILED, log.status[0]);
    TEST_ASSERT_FALSE(file_exists(TEMP_OUT_DIR "/ACC-BLOCKED.pdf.tmp"));
    rmdir(TEMP_OUT_DIR "/ACC-BLOCKED.pdf");
}

/* Layout-specific rules */
void test_bank_statement_without_interest_rate_is_valid(void) {
    ProgressLog log = {0};
    TEST_ASSERT_EQUAL_INT(1, run(PDF_REPORT_TYPE_BANK_STATEMENT,
                                 "[{" B_META "," T_CUST
                                 ",\"account\":{\"account_number\":\"ACC-1\","
                                 "\"account_type\":\"Checking\"}," B_SUM
                                 "," TXS(TX_OK) "}]",
                                 &log));
    TEST_ASSERT_TRUE(pdf_looks_valid(TEMP_OUT_DIR "/STMT-1.pdf"));
}

void test_tax_report_without_interest_rate_is_rejected(void) {
    ProgressLog log = {0};
    int         r   = run(PDF_REPORT_TYPE_TAX_REPORT,
                          "[{" TAX_BODY(T_ACC_NO_RATE("ACC-1"), TXS("")) "}]", &log);
    assert_single_rejection(r, &log, "account.interest_rate_pct");
}

void test_tax_json_fed_to_bank_layout_is_rejected(void) {
    ProgressLog log = {0};
    int         r   = run(PDF_REPORT_TYPE_BANK_STATEMENT,
                          "[{" TAX_BODY(T_ACC("ACC-1"), TXS("")) "}]", &log);
    assert_single_rejection(r, &log, "metadata.statement_id");
}

/* Signing setup */
void test_missing_pfx_fails_whole_batch_before_writing_anything(void) {
    FILE* f = fopen(TEMP_JSON_FILE, "wb");
    fputs("[{" TAX_BODY(T_ACC("ACC-1"), TXS("")) "}]", f);
    fclose(f);
    int r = pdf_engine_generate_and_sign(
        TEMP_JSON_FILE, PDF_REPORT_TYPE_TAX_REPORT, TEMP_OUT_DIR,
        "no_such_cert.pfx", "pw", NULL, NULL);
    TEST_ASSERT_EQUAL_INT(PDF_ENGINE_ERROR_SIGNING_FAILED, r);
    TEST_ASSERT_EQUAL_INT(0, count_entries(TEMP_OUT_DIR));
}

int main(void) {
    UNITY_BEGIN();
    RUN_TEST(test_empty_transactions_array_is_valid);
    RUN_TEST(test_transaction_without_description_is_valid);
    RUN_TEST(test_success_reports_non_null_empty_detail);
    RUN_TEST(test_non_ascii_names_render_without_failing);
    RUN_TEST(test_many_transactions_paginate);
    RUN_TEST(test_missing_customer_name_is_rejected_with_field_name);
    RUN_TEST(test_wrong_type_is_rejected);
    RUN_TEST(test_null_value_counts_as_missing);
    RUN_TEST(test_empty_required_string_is_rejected);
    RUN_TEST(test_missing_transactions_array_is_rejected);
    RUN_TEST(test_bad_transaction_row_names_index_and_field);
    RUN_TEST(test_empty_object_is_rejected);
    RUN_TEST(test_bad_report_in_batch_does_not_stop_the_others);
    RUN_TEST(test_rejected_report_is_identifiable_by_position);
    RUN_TEST(test_failed_publish_leaves_no_temp_file);
    RUN_TEST(test_bank_statement_without_interest_rate_is_valid);
    RUN_TEST(test_tax_report_without_interest_rate_is_rejected);
    RUN_TEST(test_tax_json_fed_to_bank_layout_is_rejected);
    RUN_TEST(test_missing_pfx_fails_whole_batch_before_writing_anything);
    return UNITY_END();
}
