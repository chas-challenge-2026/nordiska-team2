# API reference

The entire public surface is in [`include/pdf_engine.h`](../include/pdf_engine.h). Doxygen-generated HTML is available via `just docs`.

## `pdf_engine_generate_and_sign`

```c
int pdf_engine_generate_and_sign(
    const char* json_file_path, PdfReportType report_type, const char* out_dir,
    const char* pfx_path, const char* password,
    PdfEngineProgressCb progress_cb, void* progress_ctx);
```

Streams a JSON array of reports and writes one PDF per valid report into `out_dir`, signed if a PFX is provided.

| Parameter | Description |
|---|---|
| `json_file_path` | File containing a root-level array of report objects. See [input format](input-format.md). |
| `report_type` | Layout applied to every report in the file. |
| `out_dir` | Output directory. Created if missing (one level only; the parent must exist). |
| `pfx_path` | Optional `.pfx`/`.p12` used to sign every PDF. `NULL` or `""` = unsigned. |
| `password` | Optional PFX password. `NULL` if none. |
| `progress_cb` | Optional per-report callback. |
| `progress_ctx` | Passed through to `progress_cb`. |

**Return value**

- `>= 0`: number of reports successfully generated. This can be `0` if every report failed individually. Use the callback to tell which failed and why.
- `< 0`: a fatal, whole-batch `PdfEngineResult`.

Output files are named `<out_dir>/<id>.pdf`, where the id is derived per report type (see [input format](input-format.md#output-filenames)).

## Report types

```c
typedef enum {
    PDF_REPORT_TYPE_TAX_REPORT     = 0,  // Official Annual Tax Report
    PDF_REPORT_TYPE_BANK_STATEMENT = 1,  // Account statement (kontoutdrag)
} PdfReportType;
```

## Result codes

| Code | Name | Scope | Meaning |
|---:|---|---|---|
| 0 | `PDF_ENGINE_SUCCESS` | both | OK |
| -1 | `PDF_ENGINE_ERROR_INVALID_ARGS` | batch | A required argument was NULL or empty |
| -2 | `PDF_ENGINE_ERROR_NO_REPORT_OBJECT` | batch | The array contained no objects |
| -3 | `PDF_ENGINE_ERROR_JSON_READ_FAILED` | batch | File unreadable or structurally malformed |
| -4 | `PDF_ENGINE_ERROR_OUT_OF_MEMORY` | batch | Allocation failure |
| -5 | `PDF_ENGINE_ERROR_UNKNOWN_REPORT_TYPE` | batch | No layout registered for `report_type` |
| -6 | `PDF_ENGINE_ERROR_GENERATION_FAILED` | both | Rendering failed, or the output directory could not be created or written |
| -7 | `PDF_ENGINE_ERROR_SIGNING_FAILED` | both | PFX could not be loaded (batch) or signing a PDF failed (report) |
| -8 | `PDF_ENGINE_ERROR_MISSING_FIELD` | report | Missing or wrong-typed field, or no usable id. See `detail` |
| -9 | `PDF_ENGINE_ERROR_MALFORMED_REPORT` | report | Object is not valid JSON |

**Important:** when the return value is `>= 0`, the batch ran, but individual reports may have failed. Always provide a callback if you need to know which.

## Progress callback

```c
typedef struct {
    const char* report_id;  // sanitized filename stem, or "report_<index>" if none
    int         status;     // PDF_ENGINE_SUCCESS or a per-report error code
    const char* detail;     // human-readable reason; "" on success; never NULL
} PdfEngineReportResult;

typedef void (*PdfEngineProgressCb)(const PdfEngineReportResult* result,
                                    void* user_ctx);
```

- Invoked once per report object, after it has been written or has failed.
- Strings are valid **only during the callback**; copy them if you need them later.
- When `report_id` is a positional `report_<index>` name, no file was written.
- Do not throw (C++/.NET) across the callback boundary.

## Calling from .NET

The package ships `pdf_engine.dll` / `libpdf_engine.so` under the runtime-specific native asset folders, so `DllImport("pdf_engine")` resolves on both platforms.

```csharp
using System.Runtime.InteropServices;

public enum PdfReportType { TaxReport = 0, BankStatement = 1 }

[StructLayout(LayoutKind.Sequential)]
public struct PdfEngineReportResult
{
    public IntPtr ReportId;   // const char*
    public int    Status;
    public IntPtr Detail;     // const char*
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void PdfEngineProgressCb(ref PdfEngineReportResult result, IntPtr userCtx);

internal static class Native
{
    [DllImport("pdf_engine", CallingConvention = CallingConvention.Cdecl)]
    public static extern int pdf_engine_generate_and_sign(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string jsonFilePath,
        PdfReportType reportType,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string outDir,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? pfxPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? password,
        PdfEngineProgressCb? progress,
        IntPtr progressCtx);
}

// Usage
var failures = new List<(string Id, int Status, string Detail)>();
PdfEngineProgressCb cb = (ref PdfEngineReportResult r, IntPtr _) =>
{
    if (r.Status != 0)
        failures.Add((Marshal.PtrToStringUTF8(r.ReportId)!, r.Status,
                      Marshal.PtrToStringUTF8(r.Detail)!));
};

int generated = Native.pdf_engine_generate_and_sign(
    "reports.json", PdfReportType.TaxReport, "out", "cert.pfx", "secret", cb, IntPtr.Zero);

GC.KeepAlive(cb); // keep the delegate alive for the duration of the call
```

Notes:

- Keep the delegate referenced until the call returns.
- The call blocks; run it from a background job (e.g. `Task.Run`), not a request thread.
- Treat `generated < 0` as a fatal batch error and map it to `PdfEngineResult`.
