#include "json_streamer.h"

#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>

int json_stream_array_objects(const char* file_path, JsonObjectCb cb,
                              void* user_ctx) {
    if (!file_path || !cb) {
        return -1;
    }
    FILE* f = fopen(file_path, "rb");
    if (!f) {
        return -2;
    }
    int    ch;
    int    depth     = 0;
    bool   in_string = false;
    bool   escape    = false;
    size_t buf_cap   = (size_t)32 * 1024;
    size_t buf_len   = 0;
    char*  buf       = (char*)malloc(buf_cap);
    if (!buf) {
        fclose(f);
        return -3;
    }
    int status = 0;
    while ((ch = fgetc(f)) != EOF) {
        // Ignore structural whitespace and delims at the root array level
        if (depth == 0 && (ch == '[' || ch == ']' || ch == ',' || ch == ' ' ||
                           ch == '\n' || ch == '\r' || ch == '\t')) {
            continue;
        }
        // Dynamically grow the single-object buffer as needed
        if (buf_len + 2 >= buf_cap) {
            buf_cap *= 2;
            char* new_buf = (char*)realloc(buf, buf_cap);
            if (!new_buf) {
                status = -3;
                break;
            }
            buf = new_buf;
        }
        buf[buf_len++] = (char)ch;
        if (escape) {
            escape = false;
        } else if (ch == '\\' && in_string) {
            escape = true;
        } else if (ch == '"') {
            in_string = !in_string;
        } else if (!in_string) {
            if (ch == '{') {
                depth++;
            } else if (ch == '}') {
                depth--;
                // A '}' with no matching '{' means the input is malformed;
                // stop immediately rather than continuing over bad data.
                if (depth < 0) {
                    status = -4;
                    break;
                }
                // Root object boundary reached
                if (depth == 0) {
                    buf[buf_len] = '\0';
                    int cb_res   = cb(buf, user_ctx);
                    buf_len      = 0;
                    // Early abort requested by callback
                    if (cb_res != 0) {
                        status = cb_res;
                        break;
                    }
                }
            }
        }
    }
    // Distinguish a genuine read error from a clean end-of-file.
    if (status == 0 && ferror(f)) {
        status = -6;
    }
    // File ended (or loop broke) with an object still open: truncated input.
    if (status == 0 && depth != 0) {
        status = -5;
    }
    free(buf);
    fclose(f);
    return status;
}
