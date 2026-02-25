FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN dotnet tool install --global dotnet-ef

ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /src

EXPOSE 5268

ENTRYPOINT ["dotnet", "watch", "run", "--project", "DeliverTableServer", "--non-interactive", "--no-launch-profile"]
