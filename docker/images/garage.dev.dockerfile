FROM dxflrs/garage:v2.2.0 AS garage

FROM alpine:3.21

RUN apk add --no-cache curl

COPY --from=garage /garage /usr/local/bin/garage

COPY docker/config/dev/garage/garage.toml /etc/garage/garage.toml
COPY docker/config/dev/garage/init-garage.sh /init-garage.sh
RUN chmod +x /init-garage.sh

ENV GARAGE_CONFIG_FILE=/etc/garage/garage.toml

EXPOSE 3900 3901 3903

ENTRYPOINT ["/init-garage.sh"]
