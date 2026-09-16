#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#    include <direct.h>
#    include <sys/stat.h> /* struct _stat / _S_IFDIR used below */
#    include <windows.h>
#else
#    include <sys/stat.h>
#    include <sys/types.h>
#    include <unistd.h>
#endif

/**
 * @brief Creates `path` recursively (creates parent directories as needed).
 * Cross-platform (Windows & Linux/POSIX).
 */
int ensure_directory_exists(const char* path) {
    if (!path || !*path) {
        return -1;
    }

    size_t len  = strlen(path);
    char*  temp = (char*)malloc(len + 1);
    if (!temp) {
        return -1;
    }

    /* Safe string copy: length is checked and exact memory allocated */
    memcpy(temp, path, len + 1);

    for (char* p = temp + 1; *p; p++) {
        if (*p == '/' || *p == '\\') {
            char ch = *p;
            *p      = '\0';

            /* Skip empty components caused by multiple consecutive slashes */
            if (p > temp && *(p - 1) == '\0') {
                *p = ch;
                continue;
            }

/* Windows edge cases: skip "C:" drive root and UNC leading "\\ " */
#ifdef _WIN32
            if ((p - temp == 2 && temp[1] == ':') ||
                (p - temp == 1 && temp[0] == '\0')) {
                *p = ch;
                continue;
            }
#else
            /* POSIX edge case: skip leading root "/" */
            if (p == temp + 1 && temp[0] == '\0') {
                *p = ch;
                continue;
            }
#endif

#ifdef _WIN32
            if (_mkdir(temp) != 0 && errno != EEXIST) {
                struct _stat st;
                if (_stat(temp, &st) != 0 || !(st.st_mode & _S_IFDIR)) {
                    free(temp);
                    return -1;
                }
            }
#else
            if (mkdir(temp, 0755) != 0 && errno != EEXIST) {
                struct stat st;
                if (stat(temp, &st) != 0 || !S_ISDIR(st.st_mode)) {
                    free(temp);
                    return -1;
                }
            }
#endif
            *p = ch;
        }
    }

    /* Create the final path component (if not ending in a trailing slash) */
    int result = 0;
#ifdef _WIN32
    if (_mkdir(temp) != 0 && errno != EEXIST) {
        struct _stat st;
        if (_stat(temp, &st) != 0 || !(st.st_mode & _S_IFDIR)) {
            result = -1;
        }
    }
#else
    if (mkdir(temp, 0755) != 0 && errno != EEXIST) {
        struct stat st;
        if (stat(temp, &st) != 0 || !S_ISDIR(st.st_mode)) {
            result = -1;
        }
    }
#endif

    free(temp);
    return result;
}

int replace_file(const char* tmp_path, const char* out_path) {
#ifdef _WIN32
    /* Plain rename() on Windows fails if out_path already exists.
     * MoveFileExA with MOVEFILE_REPLACE_EXISTING gives POSIX rename()
     * semantics. MOVEFILE_WRITE_THROUGH waits for the move to hit disk
     * before returning, matching the "safe to assume it's there" use
     * we're making of the return value. */
    if (MoveFileExA(tmp_path, out_path,
                    MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH)) {
        return 0;
    }
    return -1;
#else
    return rename(tmp_path, out_path);
#endif
}

void sanitize_filename_component(const char* raw, char* out, size_t out_cap) {
    size_t out_len = 0;
    for (const char* p = raw;
         raw != NULL && *p != '\0' && out_len + 1 < out_cap; p++) {
        char c    = *p;
        int  safe = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9') || c == '-' || c == '_';
        if (safe) {
            out[out_len] = c;
        } else {
            out[out_len] = '_';
        }
        out_len++;
    }
    out[out_len] = '\0';
}
