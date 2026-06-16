FROM postgres:18-alpine

COPY docker/config/dev/postgres/init-db.sh /docker-entrypoint-initdb.d/01-init-db.sh
COPY docker/config/dev/postgres/postgresql.conf /etc/postgresql/postgresql.conf

RUN chmod +x /docker-entrypoint-initdb.d/01-init-db.sh

CMD ["postgres", "-c", "config_file=/etc/postgresql/postgresql.conf"]
