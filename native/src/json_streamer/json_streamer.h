#pragma once
#include <stddef.h>

/**
 * @brief Callback invoked for each root-level object extracted from the JSON
 * array.
 *
 * @param json_obj_str Null-terminated JSON string representing a single object.
 * @param user_ctx     Opaque context pointer supplied by the caller.
 * @return int         0 to continue streaming, non-zero to abort execution.
 */
typedef int (*JsonObjectCb)(const char* json_obj_str, void* user_ctx);

/**
 * @brief Streams an arbitrary JSON file containing a root-level array of
 * objects.
 *
 * Memory complexity remains O(1) proportional to the size of the largest single
 * object. Parsing stops immediately on the first malformed input encountered
 * (unbalanced braces, truncated file, or a read error) rather than continuing
 * over bad data.
 *
 * @param file_path Path to the JSON file on disk.
 * @param cb        Callback executed per isolated JSON object.
 * @param user_ctx  Optional user context passed directly to `cb`.
 *
 * @return int  0 on success.
 *              Negative values indicate a streaming error and are returned
 *              before any (further) callback invocations for this call:
 *                -1  Invalid arguments (null file_path or cb)
 *                -2  File could not be opened
 *                -3  Out of memory (malloc/realloc failure)
 *                -4  Malformed JSON: unexpected '}' with no matching '{'
 *                -5  Malformed JSON: file ended with an object still open
 *                     (unbalanced braces / truncated file)
 *                -6  I/O error while reading the file (see errno)
 *              A positive or other non-zero value is the callback's own
 *              abort code, returned verbatim.
 */
int json_stream_array_objects(const char* file_path, JsonObjectCb cb,
                              void* user_ctx);
