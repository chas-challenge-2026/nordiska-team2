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

  git-hooks.hooks = {
    clang-format = {
      enable = true;
      types_or = ["c" "c++"];
      files = "^native/";
    };

    lint = {
      enable = true;
      name = "clang-tidy static analysis";
      entry = "sh -c 'cd native && devenv shell -- just lint'";
      pass_filenames = false;
      stages = ["pre-push"];
    };

    test = {
      enable = true;
      name = "run unit tests";
      entry = "sh -c 'cd native && devenv shell -- just test'";
      pass_filenames = false;
      stages = ["pre-push"];
    };
  };

  enterShell = ''
    if [ ! -f "build/default/build.ninja" ]; then
        cmake --preset default
      fi
    if [ -f "build/default/compile_commands.json" ]; then
       ln -sf build/default/compile_commands.json .
    fi
  '';
}
