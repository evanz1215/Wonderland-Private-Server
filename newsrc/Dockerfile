# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY Directory.Build.props .
COPY Wonderland.sln .
COPY Wonderland.Domain/Wonderland.Domain.csproj Wonderland.Domain/
COPY Wonderland.Application/Wonderland.Application.csproj Wonderland.Application/
COPY Wonderland.Infrastructure/Wonderland.Infrastructure.csproj Wonderland.Infrastructure/
COPY Wonderland.GameServer/Wonderland.GameServer.csproj Wonderland.GameServer/
COPY Wonderland.WebApi/Wonderland.WebApi.csproj Wonderland.WebApi/

RUN dotnet restore Wonderland.WebApi/Wonderland.WebApi.csproj

# Copy everything and build
COPY . .
RUN dotnet publish Wonderland.WebApi/Wonderland.WebApi.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r wonderland && useradd -r -g wonderland wonderland

# Copy published output
COPY --from=build /app/publish .

# Create directories for logs and data
RUN mkdir -p /app/Logs /app/Data && chown -R wonderland:wonderland /app

USER wonderland

# Expose ports: 5000 for WebAPI/SignalR, 6414 for game client TCP
EXPOSE 5000 6414

ENV ASPNETCORE_URLS=http://+:5000
ENV DOTNET_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/api/server/status || exit 1

ENTRYPOINT ["dotnet", "Wonderland.WebApi.dll"]
