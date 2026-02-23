FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN dotnet tool install --global dotnet-reportgenerator-globaltool

ENV PATH="${PATH}:/root/.dotnet/tools"

COPY docker/config/utils/scripts/ /scripts/
RUN chmod +x /scripts/*.sh

WORKDIR /src
