#pragma once
#include <cjson/cJSON.h>
#include <hpdf.h>

/**
 * @brief PdfLayoutFn implementation for the Official Annual Tax Report.
 *
 * Expects `root` to be the report data object with (all optional)
 * `metadata`, `customer`, `account`, `summary`, and `transactions` fields.
 * Missing fields are rendered as "-" rather than failing; the transaction
 * table paginates automatically to fit however many entries are supplied.
 *
 * @param pdf  The libHaru document handle to draw into.
 * @param root The parsed report data object (see pdf_generator.h for how
 * this is produced).
 * @return int 0 on success, or a negative error code on failure.
 */
int tax_report_layout(HPDF_Doc pdf, const cJSON* root);
