# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ProductManagement.sln ./
COPY src/ProductManagement.Domain/ProductManagement.Domain.csproj             src/ProductManagement.Domain/
COPY src/ProductManagement.Application/ProductManagement.Application.csproj   src/ProductManagement.Application/
COPY src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj src/ProductManagement.Infrastructure/
COPY src/ProductManagement.API/ProductManagement.API.csproj                   src/ProductManagement.API/

RUN dotnet restore src/ProductManagement.API/ProductManagement.API.csproj

COPY src/ src/

RUN dotnet publish src/ProductManagement.API/ProductManagement.API.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductManagement.API.dll"]
