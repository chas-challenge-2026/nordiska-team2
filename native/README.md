# pdf_engine

A native C/C++ library that batch-generates **signed PDF reports** (annual tax reports and account statements) from a JSON file, built for high throughput and a small, stable C ABI.

Published as the NuGet package [`Nordiska.PdfEngine.Native`](https://www.nuget.org/packages/Nordiska.PdfEngine.Native) (Windows x64, Linux x64) and consumed from .NET via P/Invoke.

## Features

- **Streaming input.** Reads a root-level JSON array one object at a time, so memory use is O(largest report), whether the file holds 1 report or 10,000.
- **Strict validation.** Every report is checked against its layout's required fields before rendering. Bad reports are rejected with a precise reason (e.g. `missing or wrong type: account.account_number (expected non-empty string)`).
- **Fault isolation.** One failing report never aborts the batch. Per-report results are delivered through an optional progress callback.
- **Atomic output.** PDFs are written to `<id>.pdf.tmp` and renamed into place, so consumers never see half-written files.
- **Optional digital signing.** Sign every PDF with a PKCS#12 (`.pfx`/`.p12`) certificate via OpenSSL/PoDoFo.
- **Minimal C ABI.** A single exported function, `pdf_engine_generate_and_sign`. No shared memory between runtimes.
- **Self-contained release builds.** Dependencies are linked statically and symbols are restricted to the public API.

## Development environment

This project uses [**devenv**](https://devenv.sh) (Nix) so every contributor gets the same toolchain and libraries with no manual installs.

**Prerequisites:** [Nix](https://nixos.org/download) and [devenv](https://devenv.sh/getting-started/) (optionally [direnv](https://direnv.net) for automatic activation).

```bash
devenv shell        # enter the environment
direnv allow        # or: activate automatically whenever you cd into the repo
```

Entering the shell provides, pinned via `devenv.lock`:

| Category | Provided |
|---|---|
| Build | GCC/Clang, CMake ≥ 3.25, Ninja, pkg-config, `just` |
| Libraries | libharu, PoDoFo, OpenSSL 3.x, cJSON, zlib |
| Quality tools | clang-format, clang-tidy, valgrind, gdb, Doxygen |
| Automation | git hooks: clang-format on commit; lint and tests on push |

It also configures the CMake `default` preset on first entry and links `compile_commands.json` for your editor.

Not using Nix? See [manual setup](docs/building.md#manual) for the full dependency list.

## Quick start

Inside the environment:

```bash
just build          # configure and compile
just test           # run the test suite
just run            # generate sample PDFs from mock data
```

### Using the library from C

```c
#include "pdf_engine.h"
#include <stdio.h>

static void on_report(const PdfEngineReportResult* r, void* ctx) {
    int* failed = ctx;
    if (r->status != PDF_ENGINE_SUCCESS) {
        (*failed)++;
        fprintf(stderr, "%s: %s\n", r->report_id, r->detail);
    }
}

int main(void) {
    int failed = 0;
    int ok = pdf_engine_generate_and_sign(
        "reports.json", PDF_REPORT_TYPE_TAX_REPORT, "out",
        "cert.pfx", "password",      /* or NULL, NULL to leave unsigned */
        on_report, &failed);

    if (ok < 0) { fprintf(stderr, "fatal error %d\n", ok); return 1; }
    printf("%d generated, %d failed\n", ok, failed);
    return failed ? 2 : 0;
}
```

For the .NET P/Invoke binding see [docs/api.md](docs/api.md#calling-from-net).

## Documentation

| Document | Contents |
|---|---|
| [Architecture](docs/architecture.md) | Pipeline, modules, design decisions |
| [API reference](docs/api.md) | The C function, result codes, callback, .NET binding |
| [Input format](docs/input-format.md) | JSON schema per report type, filename rules, limits |
| [Building & testing](docs/building.md) | Toolchain, CMake options, `just` targets, sanitizers |
| [Extending](docs/extending.md) | Adding a new report layout |
| [Releasing](docs/releasing.md) | CI, versioning, NuGet publishing |

Generated API docs (Doxygen) can be built locally with `just docs`.

## Repository layout

```
native/
├── include/pdf_engine.h     Public C API (the only installed header)
├── src/
│   ├── pdf_engine/          Batch orchestration, validation, public entry point
│   ├── generator/           libharu document lifecycle
│   ├── layouts/             Report layouts + shared rendering helpers
│   ├── signer/              PDF signing (C++, PoDoFo + OpenSSL)
│   ├── json_streamer/       Streaming array-of-objects reader
│   ├── assets/              Embedded logo data
│   ├── logging/, utils/
├── tests/                   CTest unit/integration tests
├── examples/                Runnable examples and sample input
├── benches/                 Throughput benchmark (opt-in)
├── scripts/                 Mock data and asset generators
├── packaging/               NuGet project
├── cmake/                   Symbol map, vcpkg triplets
└── docs/                    Documentation and Doxyfile
```

## Status and goals

Version: see [`VERSION`](VERSION).

Stable packages are built from `main`; development packages (`x.y.z-dev.N`) from `dev`.
