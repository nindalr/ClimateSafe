FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ClimateSafe.Api/ClimateSafe.Api.csproj ClimateSafe.Api/
COPY ClimateSafe.Core/ClimateSafe.Core.csproj ClimateSafe.Core/
RUN dotnet restore ClimateSafe.Api/ClimateSafe.Api.csproj

COPY ClimateSafe.Api/ ClimateSafe.Api/
COPY ClimateSafe.Core/ ClimateSafe.Core/
RUN dotnet publish ClimateSafe.Api/ClimateSafe.Api.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish/ ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClimateSafe.Api.dll"]
