include(FetchContent)

FetchContent_Declare(
    unity
    URL https://github.com/ThrowTheSwitch/Unity/archive/refs/tags/v2.5.2.tar.gz
)
FetchContent_MakeAvailable(unity)

# PDF Engine tests
add_executable(test_pdf_engine test_pdf_engine.c)
target_link_libraries(test_pdf_engine PRIVATE pdf_engine unity)
add_test(NAME test_pdf_engine COMMAND test_pdf_engine)

# JSON Streamer unit tests
add_executable(test_json_streamer test_json_streamer.c)
target_link_libraries(test_json_streamer PRIVATE pdf_engine unity)
add_test(NAME test_json_streamer COMMAND test_json_streamer)
