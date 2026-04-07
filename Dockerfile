# ── Stage 1: build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files first for optimal layer caching
COPY ProductManagement.sln .
COPY src/ProductManagement.API/ProductManagement.API.csproj                         src/ProductManagement.API/
COPY src/ProductManagement.Application/ProductManagement.Application.csproj         src/ProductManagement.Application/
COPY src/ProductManagement.Domain/ProductManagement.Domain.csproj                   src/ProductManagement.Domain/
COPY src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj   src/ProductManagement.Infrastructure/
COPY tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj           tests/ProductManagement.UnitTests/

RUN dotnet restore ProductManagement.sln

# Copy remaining source
COPY . .

RUN dotnet build ProductManagement.sln -c Release --no-restore

RUN dotnet publish src/ProductManagement.API -c Release --no-build -o /app/publish

# ── Stage 2: runtime ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

# Create logs directory with correct ownership
RUN mkdir -p logs && chown -R appuser:appgroup logs

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "ProductManagement.API.dll"]
