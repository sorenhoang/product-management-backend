#!/usr/bin/env bash
set -e

# ─────────────────────────────────────────────
# 1. Solution & projects
# ─────────────────────────────────────────────
dotnet new sln -n ProductManagement

mkdir -p src tests docs

dotnet new classlib -n ProductManagement.Domain         -o src/ProductManagement.Domain         -f net8.0
dotnet new classlib -n ProductManagement.Application    -o src/ProductManagement.Application    -f net8.0
dotnet new classlib -n ProductManagement.Infrastructure -o src/ProductManagement.Infrastructure -f net8.0
dotnet new webapi   -n ProductManagement.API            -o src/ProductManagement.API            -f net8.0 --use-controllers
dotnet new xunit    -n ProductManagement.UnitTests      -o tests/ProductManagement.UnitTests    -f net8.0

dotnet sln ProductManagement.sln add \
  src/ProductManagement.Domain/ProductManagement.Domain.csproj \
  src/ProductManagement.Application/ProductManagement.Application.csproj \
  src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj \
  src/ProductManagement.API/ProductManagement.API.csproj \
  tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj

# ─────────────────────────────────────────────
# 2. Project references
# ─────────────────────────────────────────────
dotnet add src/ProductManagement.Application/ProductManagement.Application.csproj \
  reference src/ProductManagement.Domain/ProductManagement.Domain.csproj

dotnet add src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj \
  reference src/ProductManagement.Domain/ProductManagement.Domain.csproj
dotnet add src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj \
  reference src/ProductManagement.Application/ProductManagement.Application.csproj

dotnet add src/ProductManagement.API/ProductManagement.API.csproj \
  reference src/ProductManagement.Application/ProductManagement.Application.csproj
dotnet add src/ProductManagement.API/ProductManagement.API.csproj \
  reference src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj

dotnet add tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj \
  reference src/ProductManagement.Application/ProductManagement.Application.csproj
dotnet add tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj \
  reference src/ProductManagement.Domain/ProductManagement.Domain.csproj

# ─────────────────────────────────────────────
# 3. NuGet packages
# ─────────────────────────────────────────────

# Application
dotnet add src/ProductManagement.Application/ProductManagement.Application.csproj package FluentValidation --version 11.11.0
dotnet add src/ProductManagement.Application/ProductManagement.Application.csproj package FluentValidation.DependencyInjectionExtensions --version 11.11.0
dotnet add src/ProductManagement.Application/ProductManagement.Application.csproj package Mapster --version 10.0.0
dotnet add src/ProductManagement.Application/ProductManagement.Application.csproj package Mapster.DependencyInjection --version 10.0.0

# Infrastructure
dotnet add src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 8.0.14
dotnet add src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj package StackExchange.Redis --version 2.8.41
dotnet add src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj package Microsoft.Extensions.Caching.StackExchangeRedis --version 8.0.14
dotnet add src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj package Serilog.AspNetCore --version 8.0.3
dotnet add src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj package Serilog.Sinks.Console --version 6.0.0
dotnet add src/ProductManagement.Infrastructure/ProductManagement.Infrastructure.csproj package Serilog.Sinks.File --version 6.0.0

# API
dotnet add src/ProductManagement.API/ProductManagement.API.csproj package Microsoft.AspNetCore.OpenApi --version 8.0.14
dotnet add src/ProductManagement.API/ProductManagement.API.csproj package Microsoft.EntityFrameworkCore.Design --version 8.0.14
dotnet add src/ProductManagement.API/ProductManagement.API.csproj package Scalar.AspNetCore --version 2.4.1

# UnitTests
dotnet add tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj package xunit --version 2.9.3
dotnet add tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj package xunit.runner.visualstudio --version 2.8.2
dotnet add tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj package Microsoft.NET.Test.Sdk --version 17.13.0
dotnet add tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj package FluentAssertions --version 7.2.0
dotnet add tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj package Moq --version 4.20.72

# ─────────────────────────────────────────────
# 4. Folder structure with .gitkeep
# ─────────────────────────────────────────────
folders=(
  "src/ProductManagement.Domain/Entities"
  "src/ProductManagement.Domain/Enums"
  "src/ProductManagement.Domain/Common"

  "src/ProductManagement.Application/Common/Interfaces"
  "src/ProductManagement.Application/Common/Exceptions"
  "src/ProductManagement.Application/DTOs/Requests"
  "src/ProductManagement.Application/DTOs/Responses"
  "src/ProductManagement.Application/Validators"
  "src/ProductManagement.Application/Services"
  "src/ProductManagement.Application/Mappings"

  "src/ProductManagement.Infrastructure/Persistence/Configurations"
  "src/ProductManagement.Infrastructure/Persistence/Migrations"
  "src/ProductManagement.Infrastructure/Repositories"
  "src/ProductManagement.Infrastructure/Caching"
  "src/ProductManagement.Infrastructure/Extensions"

  "src/ProductManagement.API/Controllers"
  "src/ProductManagement.API/Middleware"
  "src/ProductManagement.API/Extensions"

  "tests/ProductManagement.UnitTests/Services"
  "tests/ProductManagement.UnitTests/Validators"
  "tests/ProductManagement.UnitTests/Common"

  "docs"
)

for folder in "${folders[@]}"; do
  mkdir -p "$folder"
  touch "$folder/.gitkeep"
done

# Remove default boilerplate files
rm -f src/ProductManagement.Domain/Class1.cs
rm -f src/ProductManagement.Application/Class1.cs
rm -f src/ProductManagement.Infrastructure/Class1.cs
rm -f tests/ProductManagement.UnitTests/UnitTest1.cs

echo ""
echo "Scaffold complete. Run: dotnet build ProductManagement.sln"
