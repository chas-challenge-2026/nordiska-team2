# Releasing

Releases are automated by `.github/workflows/native-nuget.yml`.

## Branches and versions

| Branch | Package version | Feed |
|---|---|---|
| `main` | `VERSION` (e.g. `0.1.0`) | GitHub Packages + NuGet.org |
| `dev` | `VERSION-dev.<run_number>` (e.g. `0.1.0-dev.42`) | GitHub Packages + NuGet.org |
| other (manual dispatch) | `VERSION-<branch>.<run_number>` | One-off test build |

`<run_number>` is monotonic, so `0.1.0-dev.41 < 0.1.0-dev.42 < 0.1.0` under SemVer ordering.

## Release procedure

1. Make changes under `native/`.
2. **Bump `native/VERSION`** following SemVer:
   - Patch: bug fixes, no API change
   - Minor: backwards-compatible additions (e.g. new report type)
   - Major: any ABI-breaking change
3. Open a PR to `main`. The `guard-version-bump` job fails the PR if `native/**` changed without a `VERSION` bump. Pushes use `--skip-duplicate`, so a forgotten bump would silently publish nothing. Mark this check as required in branch protection to enforce it.
4. Merge. CI builds, verifies, packs and publishes.

## Pipeline

```
version ─┬─ build-windows ─┐
         └─ build-linux  ──┴─ pack-and-push
```

- **build-windows**: MSVC, vcpkg triplet `x64-windows-static-md-release`; verifies `pdf_engine_generate_and_sign` is exported.
- **build-linux**: Ubuntu 22.04 (fixes the glibc 2.35 floor), triplet `x64-linux-pic`. The self-containment check fails the build if:
  - the `.so` depends on anything beyond libc-family libraries,
  - OpenSSL symbols leak into the dynamic symbol table, or
  - the public symbol is missing.
- **pack-and-push**: `dotnet pack packaging/PdfEngine.Native.csproj`, then push to GitHub Packages and NuGet.org (OIDC trusted publishing, no long-lived API key).

Both build jobs use a vcpkg binary cache keyed on the manifest, vcpkg commit and (on Windows) the runner image version. Dependency versions are pinned by `VCPKG_COMMIT` in the workflow.

## Testing a package without releasing

Use **Actions → Native NuGet → Run workflow** on your branch. The resulting package has a branch-tagged prerelease version.

## Package contents

The NuGet package bundles one native library per runtime (`win-x64`, `linux-x64`). Its readme is `packaging/PACKAGE_README.md`.
