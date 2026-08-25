{pkgs, ...}: {
  packages = with pkgs; [
    cmake
    ninja
    clang
    llvmPackages.clang-tools
    llvmPackages.bintools
    valgrind
    conan
    pkg-config

    #C libs
    cjson
    libharu

    just
    doxygen
  ];

  languages.c.enable = true;

  enterShell = ''
    if [ ! -f "build/default/build.ninja" ]; then
        cmake --preset default
      fi
    if [ -f "build/default/compile_commands.json" ]; then
       ln -sf build/default/compile_commands.json .
    fi
  '';
}
