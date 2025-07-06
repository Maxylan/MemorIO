# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /webapi

VOLUME projects/memorio-api/. .

RUN dotnet restore
RUN dotnet watch
