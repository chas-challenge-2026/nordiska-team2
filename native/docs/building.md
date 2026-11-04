# Building & testing

## Environment

### Recommended: devenv (Nix)

[`devenv`](https://devenv.sh) provisions the compiler, CMake, Ninja, clang-tools, valgrind, gdb, and all C libraries.

```bash
devenv shell      # enter the environment
direnv allow      # or switch automatically on `cd`
```

On first entry it runs `cmake --preset default` and links `compile_commands.json` for editor tooling. Pre-push git hooks run `just lint` and `just test`; clang-format runs on commit.

### Manual

| Requirement | Notes |
|---|---|
| C11 / C++17 compiler | GCC or Clang |
| CMake ≥ 3.25, Ninja, pkg-config, `just` | |
| libharu (`libhpdf`), PoDoFo, OpenSSL 3.x, cJSON, zlib | |
| clang-format, clang-tidy, valgrind | Dev tooling |

Dependency discovery works in two modes with the same `CMakeLists.txt`:

- **vcpkg manifest mode** (CI, static builds): everything via CMake config packages (`vcpkg.json`).
- **devenv / Nix**: cJSON falls back to `pkg-config`.

## Everyday commands

```bash
just build         # configure + build (Ninja, "default" preset)
just test          # build, then run CTest
just run           # build, generate mock data, run the tax-report example
just run generate_bank_statement_example
just fmt           # clang-format all tracked .c/.h
just fmt-check     # verify formatting (CI-style)
just lint          # clang-tidy over the library
just docs          # Doxygen HTML into build/default/docs/html
just clean         # remove build/ and data/
```

Run `just` with no arguments to list every target.

## CMake options

| Option | Default | Purpose |
|---|---|---|
| `BUILD_TESTING` | `ON` | Build and register tests |
| `BUILD_EXAMPLES` | `ON` | Build `examples/` |
| `BUILD_BENCHMARKS` | `OFF` | Build `benches/` (`just bench`) |
| `PDF_ENGINE_CLANG_TIDY` | `OFF` | Run clang-tidy during compilation |
| `PDF_ENGINE_RESTRICT_SYMBOLS` | `OFF` | Hide all but `pdf_engine_*`, link libstdc++ statically (release/distribution builds) |

Presets are defined in `CMakePresets.json` (`default`, `asan`).

## Tests

Tests live in `tests/` and run through CTest:

| Test | Covers |
|---|---|
| `test_json_streamer.c` | Streaming, malformed and truncated input |
| `test_pdf_engine.c` | End-to-end batch generation |
| `test_pdf_engine_validation.c` | Required-field validation and per-report errors |
| `test_pdf_signer.cpp` | Signing with a test certificate |

## Memory safety

```bash
just valgrind                         # leak check on the example binary
just valgrind-all                     # full leak kinds + origin tracking
just asan                             # build with AddressSanitizer and run
just gdb                              # debugger session
```

Each accepts an example name and arguments, e.g. `just valgrind generate_bank_statement_example`.

## Test certificate

```bash
just generate-cert    # creates data/test_cert.pfx (password: secret123)
```

This self-signed certificate is for **local development only**. Never use it, or commit its output, for real documents.

## Benchmarks

```bash
just bench
```

Builds `bench_pdf_batch` and runs it against generated mock data. The target is 10,000 customers × ~50 transactions in under 5 minutes.
