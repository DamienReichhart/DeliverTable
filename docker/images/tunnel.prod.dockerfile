# ── Stage 1: Build healthcheck ────────────────────────────────
FROM golang:1.26-alpine AS tools

WORKDIR /src
COPY docker/images/tools/go.mod        ./
COPY docker/images/tools/healthcheck/  ./healthcheck/

RUN CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o /out/healthcheck ./healthcheck

# ── Stage 2: Extract cloudflared binary ───────────────────────
FROM cloudflare/cloudflared:latest AS upstream

# ── Stage 3: Assemble rootfs ─────────────────────────────────
# cloudflared is a statically-linked Go binary; it only needs CA certs
# for TLS to the Cloudflare edge.
FROM alpine:3.24 AS rootfs

RUN apk add --no-cache ca-certificates && \
    mkdir -p \
        /rootfs/usr/local/bin \
        /rootfs/etc/ssl/certs \
        /rootfs/etc \
        /rootfs/tmp && \
    cp /etc/ssl/certs/ca-certificates.crt /rootfs/etc/ssl/certs/ && \
    printf 'root:x:0:0:root:/root:/sbin/nologin\nnonroot:x:65532:65532::/home/nonroot:/sbin/nologin\n' \
        > /rootfs/etc/passwd && \
    printf 'root:x:0:\nnonroot:x:65532:\n' \
        > /rootfs/etc/group

COPY --from=upstream /usr/local/bin/cloudflared /rootfs/usr/local/bin/cloudflared
COPY --from=tools    /out/healthcheck           /rootfs/healthcheck

# ── Stage 4: Scratch ─────────────────────────────────────────
FROM scratch

COPY --from=rootfs /rootfs /

USER 65532:65532

# Token is read from the TUNNEL_TOKEN environment variable at runtime.
ENTRYPOINT ["/usr/local/bin/cloudflared", "tunnel"]
CMD ["--no-autoupdate", "run"]
