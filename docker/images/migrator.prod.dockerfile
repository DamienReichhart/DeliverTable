# ── Stage 1: Build Go tools ───────────────────────────────────
# depcopier scans ELF binaries for shared-lib deps and assembles a rootfs.
FROM golang:1.24-alpine AS tools

WORKDIR /src
COPY docker/images/tools/ ./

RUN CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o /out/depcopier ./depcopier

# ── Stage 2: Build the .NET application ───────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY DeliverTableMigrator/DeliverTableMigrator.csproj                 DeliverTableMigrator/
COPY DeliverTableInfrastructure/DeliverTableInfrastructure.csproj     DeliverTableInfrastructure/
COPY DeliverTableSharedLibrary/DeliverTableSharedLibrary.csproj       DeliverTableSharedLibrary/

RUN dotnet restore DeliverTableMigrator/DeliverTableMigrator.csproj \
    -r linux-musl-x64

COPY DeliverTableMigrator/        DeliverTableMigrator/
COPY DeliverTableInfrastructure/  DeliverTableInfrastructure/
COPY DeliverTableSharedLibrary/   DeliverTableSharedLibrary/

RUN dotnet publish DeliverTableMigrator/DeliverTableMigrator.csproj \
    -c Release \
    -r linux-musl-x64 \
    --self-contained \
    -o /app/publish \
    --no-restore

# ── Stage 3: Assemble the minimal rootfs ─────────────────────
# Alpine provides the same musl-based shared libraries the self-contained
# .NET publish links against (libstdc++, libssl, zlib, etc.).
FROM alpine:3.21 AS rootfs

RUN apk add --no-cache \
    ca-certificates \
    libgcc \
    libstdc++ \
    libssl3 \
    libcrypto3 \
    zlib

COPY --from=tools /out/depcopier   /tools/depcopier
COPY --from=build /app/publish     /staging/app

RUN /tools/depcopier \
    --scan  /staging/app \
    --copy  /staging/app:/app \
    --copy  /usr/lib/libssl.so.3:/usr/lib/libssl.so.3 \
    --copy  /usr/lib/libcrypto.so.3:/usr/lib/libcrypto.so.3 \
    --certs \
    --user  1654:1654:appuser \
    --mkdir /tmp \
    --out   /rootfs

# ── Stage 4: Scratch ─────────────────────────────────────────
# One-shot job: applies EF Core migrations then seeds the bootstrap admin and exits.
FROM scratch

COPY --from=rootfs /rootfs /

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

USER 1654:1654

ENTRYPOINT ["/app/DeliverTableMigrator"]
