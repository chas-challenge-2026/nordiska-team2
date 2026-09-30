# Nordiska.PdfEngine.Native

Native `pdf_engine` library for **Windows x64** and **Linux x64**: batch PDF generation from JSON, with optional digital signing. This is a native-only package with no managed code. It ships the binaries so that `[DllImport("pdf_engine")]` resolves without extra configuration.

## What's inside

| Platform    | File               | Location in package          |
|-------------|--------------------|------------------------------|
| Windows x64 | `pdf_engine.dll`   | `runtimes/win-x64/native/`   |
| Linux x64   | `libpdf_engine.so` | `runtimes/linux-x64/native/` |

All third-party dependencies are statically linked, so no system packages are required.

**Requirements**
- Linux: glibc 2.35 or newer (Ubuntu 22.04+, Debian 12+ or equivalent)
- Windows: x64
- Any project that can reference .NET Standard 2.0 (including .NET 8)

## API overview

The library exports a single function:

```c
int pdf_engine_generate_and_sign(
    const char* json_file_path, PdfReportType report_type, const char* out_dir,
    const char* pfx_path, const char* password,
    PdfEngineProgressCb progress_cb, void* progress_ctx);
```

It streams a JSON file containing a root-level array of report objects and writes one PDF per report to `out_dir/<id>.pdf`. Memory use stays constant regardless of how many reports the file holds.

- **Validation:** each report is checked for required fields before rendering. Invalid reports produce no PDF and are reported as failures.
- **Isolation:** one failing report never stops the batch.
- **Signing:** pass a `.pfx`/`.p12` path (and password, if any) to sign every PDF. Pass `null` to leave them unsigned.
- **Output directory:** created if missing, but only one level deep. Its parent must already exist.
- **Return value:** the number of reports generated (`>= 0`, and `0` is possible if every report failed), or a negative error code if the whole batch failed.

### Report types

| Value | Name                             | Layout                       |
|-------|----------------------------------|------------------------------|
| `0`   | `PDF_REPORT_TYPE_TAX_REPORT`     | Annual tax report            |
| `1`   | `PDF_REPORT_TYPE_BANK_STATEMENT` | Account statement (kontoutdrag) |

### Result codes

| Code | Name                                 | Scope     | Meaning                                         |
|------|--------------------------------------|-----------|-------------------------------------------------|
| `0`  | `PDF_ENGINE_SUCCESS`                 | both      | Success                                         |
| `-1` | `PDF_ENGINE_ERROR_INVALID_ARGS`      | batch     | A required argument was null or empty           |
| `-2` | `PDF_ENGINE_ERROR_NO_REPORT_OBJECT`  | batch     | The root array contained no objects             |
| `-3` | `PDF_ENGINE_ERROR_JSON_READ_FAILED`  | batch     | File couldn't be opened or is malformed         |
| `-4` | `PDF_ENGINE_ERROR_OUT_OF_MEMORY`     | batch     | Out of memory                                   |
| `-5` | `PDF_ENGINE_ERROR_UNKNOWN_REPORT_TYPE` | batch   | No layout registered for the report type        |
| `-6` | `PDF_ENGINE_ERROR_GENERATION_FAILED` | per report | Layout or rendering failed                     |
| `-7` | `PDF_ENGINE_ERROR_SIGNING_FAILED`    | per report | A PFX was supplied but signing failed          |
| `-8` | `PDF_ENGINE_ERROR_MISSING_FIELD`     | per report | Missing or wrong-typed field; see `detail`     |
| `-9` | `PDF_ENGINE_ERROR_MALFORMED_REPORT`  | per report | Report entry isn't a valid JSON object         |

Batch-scope codes are returned by the function itself. Per-report codes arrive through the progress callback.

## Usage (C#)

```csharp
using System;
using System.Runtime.InteropServices;

public enum PdfReportType
{
    TaxReport = 0,
    BankStatement = 1,
}

[StructLayout(LayoutKind.Sequential)]
internal struct PdfEngineReportResult
{
    public IntPtr ReportId; // const char*, valid only during the callback
    public int Status;      // 0 or a negative PdfEngineResult
    public IntPtr Detail;   // const char*, empty string on success, never null
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void PdfEngineProgressCb(
    ref PdfEngineReportResult result, IntPtr userCtx);

internal static class PdfEngine
{
    // Use the bare name "pdf_engine". The runtime adds the platform prefix and
    // extension (pdf_engine.dll / libpdf_engine.so).
    [DllImport("pdf_engine", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int pdf_engine_generate_and_sign(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string jsonFilePath,
        PdfReportType reportType,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string outDir,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? pfxPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? password,
        PdfEngineProgressCb? progressCb,
        IntPtr progressCtx);
}

public static class Example
{
    public static void Run()
    {
        // Copy strings inside the callback; the pointers are not valid afterwards.
        PdfEngineProgressCb cb = (ref PdfEngineReportResult r, IntPtr _) =>
        {
            if (r.Status != 0)
            {
                var id = Marshal.PtrToStringUTF8(r.ReportId);
                var detail = Marshal.PtrToStringUTF8(r.Detail);
                Console.Error.WriteLine($"{id}: failed ({r.Status}) {detail}");
            }
        };

        int count = PdfEngine.pdf_engine_generate_and_sign(
            "input/bank_statement.json",
            PdfReportType.BankStatement,
            "output",
            pfxPath: null,
            password: null,
            cb,
            IntPtr.Zero);

        if (count < 0)
            throw new InvalidOperationException($"pdf_engine failed: {count}");

        Console.WriteLine($"Generated {count} PDFs");
    }
}
```

Notes:
- The call is synchronous. Keep the delegate in a local (as above) so it isn't garbage-collected while native code holds it.
- `progressCb` is optional. Without it, the return value only tells you how many reports succeeded, not which ones failed.
- The full C API and input contract are documented in [`pdf_engine.h`](https://github.com/chas-challenge-2026/nordiska-team2/blob/main/native/include/pdf_engine.h).

## Versioning

| Version pattern | Source | Meaning                  |
|-----------------|--------|--------------------------|
| `0.1.0`         | `main` | Stable release           |
| `0.1.0-dev.N`   | `dev`  | Latest development build |

To follow the newest development build:

```xml
<PackageReference Include="Nordiska.PdfEngine.Native" Version="0.1.0-dev.*" />
```

## Source

Source, issues and build pipeline: https://github.com/chas-challenge-2026/nordiska-team2
