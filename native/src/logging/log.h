// TODO: Create callback for logs so user can decide what to do with it.
#pragma once

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Severity levels for log_message(). Lower numeric value = higher
 * severity, matching common conventions (0 is most severe).
 */
typedef enum {
    LOG_LEVEL_ERROR = 0,
    LOG_LEVEL_WARN  = 1,
    LOG_LEVEL_INFO  = 2,
    LOG_LEVEL_DEBUG = 3,
} LogLevel;

/**
 * @brief Sets the minimum severity that will be printed.
 *
 * Messages below this severity (i.e. with a higher numeric value) are
 * discarded. Defaults to LOG_LEVEL_WARN, or the value of the
 * PDF_ENGINE_LOG_LEVEL environment variable ("ERROR"|"WARN"|"INFO"|"DEBUG")
 * if set, whichever is read first.
 */
void log_set_level(LogLevel level);

/**
 * @brief Writes a single log line to stderr if `level` is at or above the
 * current minimum severity. Not intended to be called directly; use the
 * LOG_ERROR/LOG_WARN/LOG_INFO/LOG_DEBUG macros instead.
 */
void log_message(LogLevel level, const char* file, int line, const char* fmt,
                 ...);

#define LOG_ERROR(...)                                                         \
    log_message(LOG_LEVEL_ERROR, __FILE__, __LINE__, __VA_ARGS__)
#define LOG_WARN(...)                                                          \
    log_message(LOG_LEVEL_WARN, __FILE__, __LINE__, __VA_ARGS__)
#define LOG_INFO(...)                                                          \
    log_message(LOG_LEVEL_INFO, __FILE__, __LINE__, __VA_ARGS__)
#define LOG_DEBUG(...)                                                         \
    log_message(LOG_LEVEL_DEBUG, __FILE__, __LINE__, __VA_ARGS__)

#ifdef __cplusplus
}
#endif
