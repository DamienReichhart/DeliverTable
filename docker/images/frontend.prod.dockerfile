# ── Build stage ──────────────────────────────────────────────
# Standard SDK (not Alpine) — the bundled Dart Sass in
# AspNetCore.SassCompiler requires glibc.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY DeliverTableClient/DeliverTableClient.csproj         DeliverTableClient/
COPY DeliverTableSharedLibrary/DeliverTableSharedLibrary.csproj DeliverTableSharedLibrary/

RUN dotnet restore DeliverTableClient/DeliverTableClient.csproj

COPY DeliverTableClient/       DeliverTableClient/
COPY DeliverTableSharedLibrary/ DeliverTableSharedLibrary/

RUN dotnet publish DeliverTableClient/DeliverTableClient.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime stage ────────────────────────────────────────────
FROM nginx:1.27-alpine

RUN apk add --no-cache curl \
    && rm /etc/nginx/conf.d/default.conf

COPY docker/config/prod/nginx/frontend.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
