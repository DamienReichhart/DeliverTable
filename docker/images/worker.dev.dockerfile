FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

ENTRYPOINT ["dotnet", "watch", "run", "--project", "DeliverTableWorker", "--non-interactive", "--no-launch-profile"]
