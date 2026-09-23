cmake_minimum_required(VERSION 3.25)
project(pdf_engine VERSION 0.1.0 LANGUAGES C CXX)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Required to link static third-party archives into a shared library.
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

option(BUILD_TESTING "Build unit/integration tests" ON)

option(BUILD_BENCHMARKS "Build performance benchmarks" OFF)
if(BUILD_BENCHMARKS)
    add_subdirectory(benches)
endif()

option(BUILD_EXAMPLES "Build library examples and tools" ON)
if(BUILD_EXAMPLES)
    add_subdirectory(examples)
endif()

option(PDF_ENGINE_RESTRICT_SYMBOLS
    "Hide all symbols except the public pdf_engine_* API (release/distribution builds only)"
    OFF)

if(PDF_ENGINE_RESTRICT_SYMBOLS)
    set(CMAKE_C_VISIBILITY_PRESET hidden)
    set(CMAKE_CXX_VISIBILITY_PRESET hidden)
    set(CMAKE_VISIBILITY_INLINES_HIDDEN ON)
endif()

# ---------------------------------------------------------------------------
# Dependencies
#
# Two supported environments:
#   * vcpkg manifest mode (CI, static) -> everything found via CONFIG packages
#   * devenv / Nix (local dev, shared) -> cJSON comes from pkg-config
# The same CMakeLists handles both by preferring CONFIG and falling back.
# ---------------------------------------------------------------------------

find_package(OpenSSL REQUIRED)
find_package(ZLIB REQUIRED)
find_package(podofo CONFIG REQUIRED)

find_package(cJSON CONFIG QUIET)
if(cJSON_FOUND)
    if(TARGET cjson::cjson)
        set(PDF_ENGINE_CJSON cjson::cjson)
    else()
        set(PDF_ENGINE_CJSON cjson)
    endif()
else()
    find_package(PkgConfig REQUIRED)
    pkg_check_modules(CJSON REQUIRED IMPORTED_TARGET libcjson)
    set(PDF_ENGINE_CJSON PkgConfig::CJSON)
endif()

find_path(HPDF_INCLUDE_DIR hpdf.h REQUIRED)
find_library(HPDF_LIBRARY
    NAMES hpdf_static libhpdf_static hpdf libhpdf
    REQUIRED
)

if(NOT TARGET hpdf)
    add_library(hpdf UNKNOWN IMPORTED)
    set_target_properties(hpdf PROPERTIES
        IMPORTED_LOCATION "${HPDF_LIBRARY}"
        INTERFACE_INCLUDE_DIRECTORIES "${HPDF_INCLUDE_DIR}"
    )
endif()

find_package(Doxygen)
if(DOXYGEN_FOUND)
    add_custom_target(docs
        COMMAND ${DOXYGEN_EXECUTABLE} ${CMAKE_CURRENT_SOURCE_DIR}/docs/Doxyfile
        WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
        COMMENT "Generating API documentation with Doxygen"
    )
endif()

add_subdirectory(src)

if(BUILD_TESTING)
    enable_testing()
    add_subdirectory(tests)
endif()
