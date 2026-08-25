# Core Static/Shared Library
add_library(pdf_gen_lib
    pdf_generator.c
)

target_include_directories(pdf_gen_lib
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/../include>
        $<INSTALL_INTERFACE:include>
)

# Primary PDF Generator CLI
add_executable(pdf_gen_cli bin/pdf_gen_main.c)
target_link_libraries(pdf_gen_cli PRIVATE pdf_gen_lib)
