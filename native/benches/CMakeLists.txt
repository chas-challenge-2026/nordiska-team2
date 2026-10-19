cmake_minimum_required(VERSION 3.25)
project(pdf_engine_benches CXX)

include(FetchContent)

# Fetch Nanobench via official release tarball using the same pattern as Unity
FetchContent_Declare(
    nanobench
    URL https://github.com/martinus/nanobench/archive/refs/tags/v4.3.11.tar.gz
)
FetchContent_MakeAvailable(nanobench)

add_executable(bench_pdf_batch bench_pdf_batch.cpp)

# Now the target exists, so compile definitions can be safely applied
target_compile_definitions(bench_pdf_batch PRIVATE 
    PROJECT_ROOT_DIR="${CMAKE_SOURCE_DIR}"
)

# Link core library, PoDoFo, and the fetched nanobench target
target_link_libraries(bench_pdf_batch PRIVATE pdf_engine podofo nanobench)
target_include_directories(bench_pdf_batch PRIVATE ${CMAKE_SOURCE_DIR}/src)
target_compile_features(bench_pdf_batch PRIVATE cxx_std_17)
