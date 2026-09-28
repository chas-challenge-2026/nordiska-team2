cmake_minimum_required(VERSION 3.10)

# Example stress-test binary for memory/valgrind checks
add_executable(generate_pdfs_example generate_pdfs_example.c)
target_link_libraries(generate_pdfs_example PRIVATE pdf_engine podofo)
