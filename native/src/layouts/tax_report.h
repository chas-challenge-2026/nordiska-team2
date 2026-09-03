#pragma once
#include <cjson/cJSON.h>
#include <hpdf.h>

int tax_report_layout(HPDF_Doc pdf, const cJSON* root);
