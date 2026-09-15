#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#    include <direct.h>
#else
#    include <sys/stat.h>
#    include <sys/types.h>
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
