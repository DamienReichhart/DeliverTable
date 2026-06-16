FROM redis:8-alpine

RUN addgroup -g 1656 redis-dt && \
    adduser -D -u 1656 -G redis-dt redis-dt && \
    mkdir -p /usr/local/etc/redis && \
    chown redis-dt:redis-dt /usr/local/etc/redis

COPY docker/config/prod/redis/redis.conf /usr/local/etc/redis/redis.conf
COPY docker/config/prod/redis/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh && \
    chown redis-dt:redis-dt /usr/local/etc/redis/redis.conf

USER redis-dt:redis-dt
EXPOSE 6379

ENTRYPOINT ["/docker-entrypoint.sh"]
