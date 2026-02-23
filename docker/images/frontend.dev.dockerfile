FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl inotify-tools \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

COPY docker/config/dev/scripts/frontend-entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

EXPOSE 5147

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
