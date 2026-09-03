#include "tax_report.h"

#include <stdio.h>

// TODO: implement real tax report layout and load from json
int tax_report_layout(HPDF_Doc pdf, const cJSON* root) {
    HPDF_Page page = HPDF_AddPage(pdf);
    if (!page) {
        return -1;
    }

    HPDF_Font font = HPDF_GetFont(pdf, "Helvetica", NULL);
    if (!font) {
        return -2;
    }

    HPDF_Page_SetFontAndSize(page, font, 16);

    HPDF_Page_BeginText(page);

    HPDF_Page_TextOut(page, 50, 700,
                      "Official Tax Report - Mock Data Value: 12345 SEK");

    HPDF_Page_EndText(page);

    return 0;
}
