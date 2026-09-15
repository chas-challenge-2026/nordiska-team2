{pkgs, ...}: {
  # Enable .NET 8 SDK
  languages.dotnet = {
    enable = true;
    package = pkgs.dotnet-sdk_8;
  };

  # Automatically install dotnet-ef or ensure tools are on PATH
  enterShell = ''
    export PATH="$PATH:$HOME/.dotnet/tools"

    # Check if dotnet-ef is installed globally, if not, install it for .NET 8
    if ! command -v dotnet-ef &> /dev/null; then
      echo "Installing dotnet-ef globally..."
      dotnet tool install --global dotnet-ef --version 8.0.0
    fi

    echo ".NET development environment loaded."
  '';
}
