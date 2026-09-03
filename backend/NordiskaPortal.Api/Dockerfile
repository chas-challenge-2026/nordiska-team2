# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NordiskaPortal.Api.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
        build-essential cmake git ca-certificates \
        libpng-dev zlib1g-dev \
        libcjson1 libssl3 zlib1g libpng16-16 \
    && git clone --depth 1 --branch v2.4.4 https://github.com/libharu/libharu.git /tmp/libharu \
    && cmake -S /tmp/libharu -B /tmp/libharu/build \
         -DCMAKE_BUILD_TYPE=Release \
         -DCMAKE_INSTALL_PREFIX=/usr/local \
    && cmake --build /tmp/libharu/build -j"$(nproc)" \
    && cmake --install /tmp/libharu/build \
    && echo /usr/local/lib > /etc/ld.so.conf.d/local.conf \
    && ldconfig \
    && rm -rf /tmp/libharu /var/lib/apt/lists/*

COPY --from=build /app/publish .
RUN mkdir -p /app/output

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV LD_LIBRARY_PATH=/app:/usr/local/lib
ENTRYPOINT ["dotnet", "NordiskaPortal.Api.dll"]