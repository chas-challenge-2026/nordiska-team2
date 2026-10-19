#pragma once

#include "pdf_layout.h"

#ifdef __cplusplus
extern "C" {
#endif

/** @brief Layout configuration bundle for the Account Statement (Kontoutdrag).
 */
const PdfLayoutConfig* bank_statement_get_config(void);

#ifdef __cplusplus
}
#endif
