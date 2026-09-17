FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Verity.sln ./
COPY src/Verity.Domain/Verity.Domain.csproj src/Verity.Domain/
COPY src/Verity.Application/Verity.Application.csproj src/Verity.Application/
COPY src/Verity.Infrastructure/Verity.Infrastructure.csproj src/Verity.Infrastructure/
COPY src/Verity.Api/Verity.Api.csproj src/Verity.Api/
COPY tests/Verity.Domain.Tests/Verity.Domain.Tests.csproj tests/Verity.Domain.Tests/
COPY tests/Verity.Api.Tests/Verity.Api.Tests.csproj tests/Verity.Api.Tests/
RUN dotnet restore Verity.sln

COPY . .
RUN dotnet publish src/Verity.Api/Verity.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is needed for the compose healthcheck; the base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5080
EXPOSE 5080

ENTRYPOINT ["dotnet", "Verity.Api.dll"]
