# ── Build stage ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY DeliverTableServer/DeliverTableServer.csproj               DeliverTableServer/
COPY DeliverTableSharedLibrary/DeliverTableSharedLibrary.csproj DeliverTableSharedLibrary/

RUN dotnet restore DeliverTableServer/DeliverTableServer.csproj

COPY DeliverTableServer/       DeliverTableServer/
COPY DeliverTableSharedLibrary/ DeliverTableSharedLibrary/

RUN dotnet publish DeliverTableServer/DeliverTableServer.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime stage ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

RUN apk add --no-cache curl

WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "DeliverTableServer.dll"]
