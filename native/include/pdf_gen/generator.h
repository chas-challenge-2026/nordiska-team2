/**
 * @file generator.h
 * @brief DOXYGEN TEST. Only exists to test build system etc.
 */

#pragma once

#include <stdbool.h>

/**
 * @brief Configuration settings for a new PDF document.
 */
typedef struct {
    const char *title;  /**< Document title displayed in properties */
    const char *author; /**< Document author metadata */
} PdfDocConfigT;

/**
 * @brief Initializes the PDF generator system.
 * @return true on success, false if initialization failed.
 */
bool pdf_gen_init(void);

/**
 * @brief Generates a PDF file at the specified path.
 *
 * @param output_path Filepath where the PDF will be saved.
 * @param config Pointer to document configuration struct.
 * @return true if PDF creation succeeded, false otherwise.
 */
bool pdf_gen_create_document(const char *output_path, const PdfDocConfigT *config);
