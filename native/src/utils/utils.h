#ifdef _WIN32
#    include <direct.h>
#    include <io.h>
#else
#    include <sys/stat.h>
#    include <sys/types.h>
#endif

/** @brief Creates `path` recursively (creates parent directories if needed). */
int ensure_directory_exists(const char* path);
