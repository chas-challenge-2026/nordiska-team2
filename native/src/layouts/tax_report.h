#pragma once

#include "pdf_layout.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Returns the layout configuration bundle for the Official Annual Tax
 * Report.
 */
const PdfLayoutConfig* tax_report_get_config(void);

#ifdef __cplusplus
}
#endif
