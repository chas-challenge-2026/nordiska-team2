add_library(pdf_engine SHARED
    generator/pdf_generator.c
    generator/pdf_generator.h
    signer/pdf_signer.c
    signer/pdf_signer.h
    layouts/tax_report.c
    layouts/tax_report.h
    pdf_engine/pdf_engine.c
)

# Export all symbols automatically on Windows MSVC for .NET P/Invoke
set_target_properties(pdf_engine PROPERTIES WINDOWS_EXPORT_ALL_SYMBOLS ON)

# Set up include paths using project root
target_include_directories(pdf_engine
    PUBLIC
        ${CMAKE_SOURCE_DIR}/include
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
)

# Link external dependencies privately to prevent leakage
if(WIN32)
    target_link_libraries(pdf_engine
        PRIVATE
            hpdf
            OpenSSL::Crypto
            cjson
            ZLIB::ZLIB
    )
else()
    target_link_libraries(pdf_engine
        PRIVATE
            hpdf
            OpenSSL::Crypto
            ${CJSON_LIBRARIES}
    )
endif()

# Executable for local CLI testing
add_executable(pdf_tax_reporter bin/pdf_tax_reporter.c)
target_link_libraries(pdf_tax_reporter PRIVATE pdf_engine)
