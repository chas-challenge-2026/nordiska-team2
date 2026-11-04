# Adding a new report layout

A report type is a self-contained module under `src/layouts/`. Use `bank_statement.c` as the reference.

## Steps

1. **Create the layout** `src/layouts/my_report.c` / `.h`.
2. **Declare the input contract**: a static `PdfRequiredField[]` listing every field the layout draws.
   - Use dot paths: `"customer.full_name"`.
   - Use `[].` to check every array element: `"transactions[].amount_sek"`.
   - Fields that are deliberately optional are *omitted* from the list and handled in the drawing code.
3. **Write `layout_fn`**: `int fn(HPDF_Doc, const cJSON* root)`, returning `0` on success. Build on the helpers in `pdf_render.h` (`render_ctx_init`, `render_title_block`, `render_section_title`, `render_key_value_rows`, `render_transactions_section`).
4. **Write `extract_id_fn`**: derive a stable, unique filename stem and pass it through `sanitize_filename_component()`. Choose an identifier that is unique per output document.
5. **Expose a config getter**:

   ```c
   const PdfLayoutConfig* my_report_get_config(void) {
       static const PdfLayoutConfig CONFIG = {
           .layout_fn            = my_report_layout,
           .extract_id_fn        = my_report_extract_id,
           .required_fields      = REQUIRED_FIELDS,
           .required_field_count = sizeof(REQUIRED_FIELDS) / sizeof(REQUIRED_FIELDS[0]),
       };
       return &CONFIG;
   }
   ```

6. **Register it**
   - Add a value to `PdfReportType` in `include/pdf_engine.h` (append; never renumber existing values, since this is a public ABI).
   - Add a `case` in `resolve_layout_config()` in `src/pdf_engine/pdf_engine.c`.
   - Add the `.c`/`.h` files to `src/CMakeLists.txt`.
7. **Test it**: add a validation case to `tests/test_pdf_engine_validation.c` and an end-to-end case to `tests/test_pdf_engine.c`.
8. **Document it**: add the field table to [input-format.md](input-format.md).
9. **Bump `VERSION`**; see [releasing](releasing.md).

## Guidelines

- Keep the contract honest: if the layout reads a field, it is either listed as required or has an explicit, documented fallback.
- Never change the signature of `pdf_engine_generate_and_sign` or existing enum values without a major version bump.
- Run `just fmt`, `just lint`, `just test` and `just valgrind` before opening a PR.
