# Сборка из корня репозитория:  docker build -f src/AuthPractice.Web/Dockerfile .
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Сначала только csproj — чтобы restore кэшировался
COPY ["nuget.config", "Directory.Build.props", "./"]
COPY ["src/AuthPractice.Web/AuthPractice.Web.csproj", "src/AuthPractice.Web/"]
COPY ["src/AuthPractice.Core/AuthPractice.Core.csproj", "src/AuthPractice.Core/"]
COPY ["src/AuthPractice.Contracts/AuthPractice.Contracts.csproj", "src/AuthPractice.Contracts/"]
COPY ["src/AuthPractice.Domain/AuthPractice.Domain.csproj", "src/AuthPractice.Domain/"]
COPY ["src/AuthPractice.Infrastructure/AuthPractice.Infrastructure.csproj", "src/AuthPractice.Infrastructure/"]
RUN dotnet restore "src/AuthPractice.Web/AuthPractice.Web.csproj"
COPY src/ src/
RUN dotnet publish "src/AuthPractice.Web/AuthPractice.Web.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuthPractice.Web.dll"]
