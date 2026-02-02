# SevilAI Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/SevilAI.Api/SevilAI.Api.csproj", "src/SevilAI.Api/"]
COPY ["src/SevilAI.Application/SevilAI.Application.csproj", "src/SevilAI.Application/"]
COPY ["src/SevilAI.Domain/SevilAI.Domain.csproj", "src/SevilAI.Domain/"]
COPY ["src/SevilAI.Infrastructure/SevilAI.Infrastructure.csproj", "src/SevilAI.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/SevilAI.Api/SevilAI.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/SevilAI.Api"
RUN dotnet build "SevilAI.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SevilAI.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "SevilAI.Api.dll"]
