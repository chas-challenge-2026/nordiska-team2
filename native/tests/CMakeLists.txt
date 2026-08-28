include(FetchContent)

FetchContent_Declare(
    unity
    URL https://github.com/ThrowTheSwitch/Unity/archive/refs/tags/v2.5.2.tar.gz
)
FetchContent_MakeAvailable(unity)

add_executable(test_pdf_engine test_pdf_engine.c)
target_link_libraries(test_pdf_engine PRIVATE pdf_engine unity)

add_test(NAME test_pdf_engine COMMAND test_pdf_engine)
