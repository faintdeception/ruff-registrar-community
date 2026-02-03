# See https://github.com/dotnet/dotnet-docker/blob/main/samples/aspnetapp/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["src/StudentRegistrar.Api/StudentRegistrar.Api.csproj", "src/StudentRegistrar.Api/"]
COPY ["src/StudentRegistrar.ServiceDefaults/StudentRegistrar.ServiceDefaults.csproj", "src/StudentRegistrar.ServiceDefaults/"]
COPY ["src/StudentRegistrar.Data/StudentRegistrar.Data.csproj", "src/StudentRegistrar.Data/"]
COPY ["src/StudentRegistrar.Models/StudentRegistrar.Models.csproj", "src/StudentRegistrar.Models/"]

# Restore dependencies
RUN dotnet restore "src/StudentRegistrar.Api/StudentRegistrar.Api.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/src/StudentRegistrar.Api"

# Build
RUN dotnet build "StudentRegistrar.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "StudentRegistrar.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StudentRegistrar.Api.dll"]
