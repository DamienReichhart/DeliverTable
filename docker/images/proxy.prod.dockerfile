# ── Stage 1: Build Go tools ───────────────────────────────────
FROM golang:1.26-alpine AS tools

WORKDIR /src
COPY docker/images/tools/ ./

RUN CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o /out/depcopier   ./depcopier   && \
    CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o /out/healthcheck ./healthcheck

# ── Stage 2: Assemble rootfs from nginx:alpine ───────────────
FROM nginx:1.31-alpine AS rootfs

COPY --from=tools /out/depcopier   /tools/depcopier
COPY --from=tools /out/healthcheck /staging/healthcheck

COPY docker/config/prod/nginx/nginx.conf /staging/nginx.conf

RUN /tools/depcopier \
    --scan  /usr/sbin/nginx \
    --copy  /usr/sbin/nginx:/usr/sbin/nginx \
    --copy  /etc/nginx/mime.types:/etc/nginx/mime.types \
    --copy  /staging/nginx.conf:/etc/nginx/nginx.conf \
    --copy  /staging/healthcheck:/healthcheck \
    --link  /dev/stdout:/var/log/nginx/access.log \
    --link  /dev/stderr:/var/log/nginx/error.log \
    --link  /var/run:/run \
    --user  101:101:nginx \
    --mkdir /var/cache/nginx \
    --mkdir /var/run \
    --mkdir /tmp \
    --out   /rootfs

# ── Stage 3: Scratch ─────────────────────────────────────────
FROM scratch

COPY --from=rootfs /rootfs /

EXPOSE 80
STOPSIGNAL SIGQUIT

ENTRYPOINT ["/usr/sbin/nginx", "-g", "daemon off;"]
