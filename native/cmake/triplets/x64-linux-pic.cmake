# Same as the built-in x64-linux triplet (static libraries, dynamic libc), but
# forces -fPIC on every port.
#
# Why: we link these static archives INTO a shared object (libpdf_engine.so).
# Anything not built position-independent fails at link time with
# "relocation R_X86_64_PC32 against symbol ... can not be used when making a
# shared object; recompile with -fPIC". Some ports (notably openssl) don't
# enable PIC for static builds on their own, so we set it globally here rather
# than discovering it one port at a time.
set(VCPKG_TARGET_ARCHITECTURE x64)
set(VCPKG_CRT_LINKAGE dynamic)
set(VCPKG_LIBRARY_LINKAGE static)
set(VCPKG_CMAKE_SYSTEM_NAME Linux)

set(VCPKG_C_FLAGS "-fPIC")
set(VCPKG_CXX_FLAGS "-fPIC")
