add_library(pdf_engine SHARED
    generator/pdf_generator.c
    generator/pdf_generator.h
    signer/pdf_signer.c
    signer/pdf_signer.h
    layouts/tax_report.c
    layouts/tax_report.h
    pdf_engine/pdf_engine.c
)

# Set up include paths so modules can find each other cleanly
target_include_directories(pdf_engine
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/../include
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
)

# external dependencies
target_link_libraries(pdf_engine
    PUBLIC
        hpdf
        OpenSSL::Crypto
        ${CJSON_LIBRARIES}
)

# executables
add_executable(pdf_tax_reporter bin/pdf_tax_reporter.c)
target_link_libraries(pdf_tax_reporter PRIVATE pdf_engine)
