FROM redis:7-alpine

RUN apk add --no-cache curl

COPY docker/config/dev/redis/redis.conf /usr/local/etc/redis/redis.conf

EXPOSE 6379

CMD ["redis-server", "/usr/local/etc/redis/redis.conf"]
