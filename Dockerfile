# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["fcg-catalog-api.csproj", "./"]
RUN dotnet restore "fcg-catalog-api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "fcg-catalog-api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "fcg-catalog-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "fcg-catalog-api.dll"]
