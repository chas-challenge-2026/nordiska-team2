#pragma once

#ifdef _WIN32
#    include <direct.h>
#    include <io.h>
#    include <sys/stat.h>
#else
#    include <sys/stat.h>
#    include <sys/types.h>
#endif

/** @brief Creates `path` recursively (creates parent directories if needed). */
int ensure_directory_exists(const char* path);

/**
 * @brief Copies `raw` into `out`, replacing anything outside [A-Za-z0-9_-]
 * with '_'.
 *
 * `raw` comes straight from the input JSON, so treating it as a trusted
 * filename component would let a malformed or malicious record write
 * outside `out_dir` (e.g. via "../"). Stripping everything but a safe
 * character set neutralizes that regardless of what `raw` contains.
 */
void sanitize_filename_component(const char* raw, char* out, size_t out_cap);

/**
 * @brief Moves `tmp_path` to `out_path`, overwriting `out_path` if it
 * already exists.
 *
 * POSIX rename() already replaces an existing destination atomically, but
 * the Windows CRT rename() fails with EEXIST-like behavior if the target
 * exists - this wrapper makes the two platforms behave the same way, which
 * matters here since report ids can repeat across runs.
 *
 * @return 0 on success, non-zero on failure.
 */
int replace_file(const char* tmp_path, const char* out_path);
