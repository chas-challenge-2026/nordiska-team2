# syntax=docker/dockerfile:1.6
#
# Local mirror of the CI "build-linux" job: Ubuntu 22.04 (glibc 2.35 floor),
# same pinned vcpkg commit, same triplet, same checks.
#
# Build context is the REPO ROOT (the one containing native/):
#   DOCKER_BUILDKIT=1 docker build -f native/Dockerfile \
#       --target out --output type=local,dest=./out .
# Result: ./out/libpdf_engine.so
FROM ubuntu:22.04 AS build
ARG VCPKG_COMMIT=301856f5f2824f788a3ffa6332293861cccd23b4
ENV DEBIAN_FRONTEND=noninteractive

# Ubuntu 22.04's apt cmake is 3.22; the project needs >= 3.25, so use pip's.
RUN apt-get update && apt-get install -y --no-install-recommends \
      build-essential ninja-build pkg-config autoconf automake \
      autoconf-archive libtool gperf bison flex nasm python3 python3-pip \
      python3-jinja2 python3-setuptools zip unzip curl git patchelf \
      ca-certificates binutils \
 && pip3 install cmake \
 && rm -rf /var/lib/apt/lists/*

RUN git clone https://github.com/microsoft/vcpkg /opt/vcpkg \
 && git -C /opt/vcpkg checkout ${VCPKG_COMMIT} \
 && /opt/vcpkg/bootstrap-vcpkg.sh -disableMetrics
ENV VCPKG_ROOT=/opt/vcpkg

WORKDIR /src
COPY native/ /src/

# The cache mount keeps compiled vcpkg packages (podofo, openssl, ...) between
# builds, so only the first build is slow.
RUN --mount=type=cache,target=/root/.cache/vcpkg-bin \
    export VCPKG_BINARY_SOURCES="clear;files,/root/.cache/vcpkg-bin,readwrite" \
 && cmake -S . -B build/release -G Ninja --no-warn-unused-cli \
      -DCMAKE_BUILD_TYPE=Release \
      -DCMAKE_TOOLCHAIN_FILE=$VCPKG_ROOT/scripts/buildsystems/vcpkg.cmake \
      -DVCPKG_OVERLAY_TRIPLETS=cmake/triplets \
      -DVCPKG_TARGET_TRIPLET=x64-linux-pic \
      -DBUILD_TESTING=OFF -DBUILD_EXAMPLES=OFF \
      -DPDF_ENGINE_RESTRICT_SYMBOLS=ON \
 && cmake --build build/release --target pdf_engine \
 && cmake --install build/release --prefix /stage --component Unspecified

# Same self-containment checks as CI, so a bad .so fails here, not on his machine.
RUN set -e; SO=/stage/libpdf_engine.so; \
    echo "--- DT_NEEDED ---"; readelf -d "$SO" | grep NEEDED || true; \
    UNEXPECTED=$(readelf -d "$SO" | grep NEEDED | sed -E 's/.*\[(.*)\]/\1/' \
      | grep -Ev 'libc\.so|libm\.so|libdl\.so|libpthread\.so|librt\.so|ld-linux' || true); \
    if [ -n "$UNEXPECTED" ]; then echo "ERROR: not self-contained: $UNEXPECTED"; exit 1; fi; \
    echo "--- max glibc ---"; objdump -T "$SO" | grep -o 'GLIBC_[0-9.]*' | sort -V -u | tail -1; \
    echo "--- exported pdf_engine_* ---"; nm -D --defined-only "$SO" | grep pdf_engine_; \
    for fn in pdf_engine_generate_and_sign pdf_engine_generate_pdfs; do \
      nm -D --defined-only "$SO" | grep -q "$fn" || { echo "ERROR: $fn not exported"; exit 1; }; \
    done

FROM scratch AS out
COPY --from=build /stage/libpdf_engine.so /
