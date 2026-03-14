# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /src

# Restore dependencies first (better layer caching)
COPY Forgefit.Api/Forgefit.Api.csproj Forgefit.Api/
RUN dotnet restore Forgefit.Api/Forgefit.Api.csproj

# Copy source and publish
COPY . .
RUN dotnet publish Forgefit.Api/Forgefit.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime image ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runner

WORKDIR /app

# Non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=builder /app/publish .

EXPOSE 3000

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Forgefit.Api.dll"]
