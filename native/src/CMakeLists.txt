add_library(pdf_engine SHARED
    generator/pdf_generator.c
    generator/pdf_generator.h
    signer/pdf_signer.cpp
    signer/pdf_signer.h
    layouts/tax_report.c
    layouts/tax_report.h
    json_streamer/json_streamer.h
    json_streamer/json_streamer.c
    logging/log.h
    logging/log.c
    utils/utils.h
    utils/utils.c
    pdf_engine/pdf_engine.c
)

target_include_directories(pdf_engine
    PUBLIC
        ${CMAKE_SOURCE_DIR}/include
        ${CMAKE_CURRENT_SOURCE_DIR}
)

target_link_libraries(pdf_engine
    PRIVATE
        hpdf
        OpenSSL::Crypto
        podofo::podofo
        ${PDF_ENGINE_CJSON}
        ZLIB::ZLIB
)

if(UNIX AND NOT APPLE)
    if(PDF_ENGINE_RESTRICT_SYMBOLS)
        target_link_options(pdf_engine PRIVATE
            # Don't impose a GLIBCXX_* floor on the consuming machine just
            # because podofo is C++. The .so then only needs glibc itself.
            -static-libstdc++
            -static-libgcc
            # Strip symbols pulled in from static archives out of the
            # dynamic symbol table, on top of the version script below.
            "LINKER:--exclude-libs,ALL"
            "LINKER:--version-script=${CMAKE_SOURCE_DIR}/cmake/pdf_engine.map"
        )
        set_target_properties(pdf_engine PROPERTIES
            LINK_DEPENDS ${CMAKE_SOURCE_DIR}/cmake/pdf_engine.map
        )
    endif()
    set_target_properties(pdf_engine PROPERTIES
        # Harmless either way, and keeps a bundled-siblings fallback working
        # if a dependency ever has to ship as a separate .so.
        BUILD_RPATH_USE_ORIGIN ON
        INSTALL_RPATH "$ORIGIN"
    )
endif()

# Executable for local CLI testing
add_executable(pdf_tax_reporter bin/pdf_tax_reporter.c)
target_link_libraries(pdf_tax_reporter PRIVATE pdf_engine)

# Lets CI do `cmake --install build --prefix stage` and get a predictable
# layout, instead of hunting for the binary with Get-ChildItem / find.
include(GNUInstallDirs)
install(TARGETS pdf_engine
    RUNTIME DESTINATION .   # .dll on Windows
    LIBRARY DESTINATION .   # .so on Linux
)
