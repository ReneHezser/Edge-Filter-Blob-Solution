FROM mcr.microsoft.com/dotnet/sdk:7.0.102-bullseye-slim AS build-env
WORKDIR /app

LABEL org.opencontainers.image.source https://github.com/ReneHezser/Edge-Filter-Blob-Solution
LABEL org.opencontainers.image.description An IoT Edge module that filters ingested data and routes to a blob storage account (which can be an IoT Edge module as well)

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:7.0.3-bullseye-slim
WORKDIR /app
COPY --from=build-env /app/out ./

RUN useradd -ms /bin/bash moduleuser
USER moduleuser

ENTRYPOINT ["dotnet", "FilterModule.dll"]