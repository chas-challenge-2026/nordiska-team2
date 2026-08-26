include(FetchContent)

FetchContent_Declare(
    unity
    URL https://github.com/ThrowTheSwitch/Unity/archive/refs/tags/v2.5.2.tar.gz
)
FetchContent_MakeAvailable(unity)

add_executable(test_generator test_generator.c)
target_link_libraries(test_generator PRIVATE pdf_gen_lib unity)

add_test(NAME GeneratorTest COMMAND test_generator)
