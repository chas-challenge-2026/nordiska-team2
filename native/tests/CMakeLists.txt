include(FetchContent)

FetchContent_Declare(
    unity
    URL https://github.com/ThrowTheSwitch/Unity/archive/refs/tags/v2.5.2.tar.gz
)
FetchContent_MakeAvailable(unity)

add_executable(test_interop_api test_interop_api.c)
target_link_libraries(test_interop_api PRIVATE pdf_engine unity)

add_test(NAME test_interop_api COMMAND test_interop_api)
