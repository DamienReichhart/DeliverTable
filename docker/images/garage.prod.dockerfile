# ── Stage 1: Build Go healthcheck ───────────────────────────────
FROM golang:1.26-alpine AS tools

WORKDIR /src
COPY docker/images/tools/ ./

RUN CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o /out/healthcheck ./healthcheck

# ── Stage 2: Extract Garage binary ─────────────────────────────
FROM dxflrs/garage:v2.3.0 AS garage

# ── Stage 3: Assemble minimal image ───────────────────────────
FROM alpine:3.24

RUN apk add --no-cache ca-certificates && \
    addgroup -g 1655 garage && \
    adduser -D -u 1655 -G garage garage && \
    mkdir -p /var/lib/garage/meta /var/lib/garage/data && \
    chown -R 1655:1655 /var/lib/garage

COPY --from=tools  /out/healthcheck            /healthcheck
COPY --from=garage /garage                     /usr/local/bin/garage

COPY docker/config/prod/garage/garage.toml     /etc/garage/garage.toml
COPY docker/config/prod/garage/init-garage.sh  /init-garage.sh
RUN chmod +x /init-garage.sh

ENV GARAGE_CONFIG_FILE=/etc/garage/garage.toml

USER 1655:1655
EXPOSE 3900 3901 3903

ENTRYPOINT ["/init-garage.sh"]
