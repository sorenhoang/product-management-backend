.PHONY: up up-build down down-volumes logs ps migrate build test format

up:
	docker compose up -d

up-build:
	docker compose up -d --build

down:
	docker compose down

down-volumes:
	docker compose down -v

logs:
	docker compose logs -f api

ps:
	docker compose ps

migrate:
	dotnet ef database update \
		--project src/ProductManagement.Infrastructure \
		--startup-project src/ProductManagement.API

build:
	dotnet build ProductManagement.sln

test:
	dotnet test tests/ProductManagement.UnitTests

format:
	dotnet format ProductManagement.sln
