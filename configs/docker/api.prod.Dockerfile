# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /source

COPY --link memorio-api/*.csproj .
RUN dotnet restore

COPY --link memorio-api/. .
RUN rm -rf ./obj
RUN rm -rf ./bin

RUN dotnet publish -o /memorio-api

# Runtime..
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine-composite

ENV RECEPTION_ENVIRONMENT=Production

WORKDIR /memorio-api
COPY --link --from=build /memorio-api .
