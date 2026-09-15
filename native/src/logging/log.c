// Must be defined before any header is included.
// The _WIN32 branch below uses localtime_s() instead,
#if !defined(_WIN32) && !defined(_POSIX_C_SOURCE)
// NOLINTNEXTLINE(bugprone-reserved-identifier)
#    define _POSIX_C_SOURCE 200809L
#endif

#include "log.h"

#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

static LogLevel g_min_level     = LOG_LEVEL_WARN;
static int      g_level_is_init = 0;

static const char* level_name(LogLevel level) {
    switch (level) {
    case LOG_LEVEL_ERROR:
        return "ERROR";
    case LOG_LEVEL_WARN:
        return "WARN";
    case LOG_LEVEL_INFO:
        return "INFO";
    case LOG_LEVEL_DEBUG:
        return "DEBUG";
    default:
        return "?";
    }
}

void log_set_level(LogLevel level) {
    g_min_level     = level;
    g_level_is_init = 1;
}

// Lazily reads PDF_ENGINE_LOG_LEVEL on the first log call so a host
// application never has to call log_set_level() to get sane defaults. This
// engine is expected to be driven single-threaded from a managed host
// (.NET P/Invoke), so the lack of synchronization here is intentional.
static void init_level_from_env_if_needed(void) {
    if (g_level_is_init) {
        return;
    }
    g_level_is_init       = 1;
    const char* env_level = getenv("PDF_ENGINE_LOG_LEVEL");
    if (!env_level) {
        return;
    }
    if (strcmp(env_level, "ERROR") == 0) {
        g_min_level = LOG_LEVEL_ERROR;
    } else if (strcmp(env_level, "WARN") == 0) {
        g_min_level = LOG_LEVEL_WARN;
    } else if (strcmp(env_level, "INFO") == 0) {
        g_min_level = LOG_LEVEL_INFO;
    } else if (strcmp(env_level, "DEBUG") == 0) {
        g_min_level = LOG_LEVEL_DEBUG;
    }
}

void log_message(LogLevel level, const char* file, int line, const char* fmt,
                 ...) {
    init_level_from_env_if_needed();
    if (level > g_min_level) {
        return;
    }

    time_t    now = time(NULL);
    struct tm tm_now;
#ifdef _WIN32
    localtime_s(&tm_now, &now);
#else
    localtime_r(&now, &tm_now);
#endif
    char timestamp[20];
    strftime(timestamp, sizeof(timestamp), "%Y-%m-%d %H:%M:%S", &tm_now);

    fprintf(stderr, "%s [%-5s] %s:%d: ", timestamp, level_name(level), file,
            line);

    va_list args;
    va_start(args, fmt);
    vfprintf(stderr, fmt, args);
    va_end(args);

    fprintf(stderr, "\n");
}
