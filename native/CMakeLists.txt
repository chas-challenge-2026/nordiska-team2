cmake_minimum_required(VERSION 3.25)
project(pdf_signer VERSION 0.1.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

option(BUILD_TESTING "Build unit/integration tests" ON)

# Find system/nix packages via pkg-config
find_package(PkgConfig REQUIRED)

# Find cjson and libHaru (hpdf) from devenv environment
pkg_check_modules(CJSON REQUIRED libcjson)

# Manually locate libharu headers and libraries from the environment
find_path(HPDF_INCLUDE_DIR hpdf.h
    REQUIRED
)

find_library(HPDF_LIBRARY NAMES hpdf libhpdf
    REQUIRED
)

# Create an imported target so you can link it cleanly like a modern library
if(NOT TARGET hpdf)
    add_library(hpdf UNKNOWN IMPORTED)
    set_target_properties(hpdf PROPERTIES
        IMPORTED_LOCATION "${HPDF_LIBRARY}"
        INTERFACE_INCLUDE_DIRECTORIES "${HPDF_INCLUDE_DIR}"
    )
endif()
# Find system OpenSSL
find_package(OpenSSL REQUIRED)

# Documentation Target
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
