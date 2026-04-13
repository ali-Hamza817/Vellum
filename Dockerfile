# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug config)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Vellum.Api/Vellum.Api.csproj", "src/Vellum.Api/"]
COPY ["src/Vellum.Cli/Vellum.Cli.csproj", "src/Vellum.Cli/"]
COPY ["src/Vellum.Ingestor/Vellum.Ingestor.csproj", "src/Vellum.Ingestor/"]
COPY ["src/Vellum.Extractor/Vellum.Extractor.csproj", "src/Vellum.Extractor/"]
COPY ["src/Vellum.Validator/Vellum.Validator.csproj", "src/Vellum.Validator/"]
COPY ["src/Vellum.Remediation/Vellum.Remediation.csproj", "src/Vellum.Remediation/"]
RUN dotnet restore "./src/Vellum.Api/Vellum.Api.csproj"
COPY . .
WORKDIR "/src/src/Vellum.Api"
RUN dotnet build "./Vellum.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Vellum.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in normal mode (Default when not using the Debug configuration)
# Use the SDK image for the final stage because we need MSBuild/Roslyn
# to analyze .NET solutions at runtime.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:6001
EXPOSE 6001
ENTRYPOINT ["dotnet", "Vellum.Api.dll"]
