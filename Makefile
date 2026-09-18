.PHONY: build build-api build-web test test-web run-api run-bff run-web migrate migrate-add clean help up down logs restore clean-containers

DOCKER_COMPOSE := docker compose
SDK_IMAGE := mcr.microsoft.com/dotnet/sdk:10.0
SDK_RUN := docker run --rm -v $(CURDIR):/src -w /src $(SDK_IMAGE)

build: build-api ## Build backend

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "%-15s %s\n", $$1, $$2}'

build-api: ## Build the backend
	dotnet build src/Api/InvoicingApi/InvoicingApi.csproj

build-web: ## Build the frontend
	cd src/web && npm run build

test: ## Run all tests (in a container, no local SDK needed)
	$(SDK_RUN) dotnet test Invoicing.Claude.Code.slnx

test-web: ## Run frontend tests (Vitest)
	cd src/web && npm test

run-api: ## Run the api, its db and the Aspire dashboard via docker compose
	$(DOCKER_COMPOSE) up --build api

run-bff: ## Run the BFF (proxies auth to the api; run alongside run-api)
	dotnet run --project src/Web/InvoicingBff

run-web: ## Run the React dev server
	cd src/web && npm run dev

migrate: ## Apply EF Core migrations
	dotnet ef database update --project src/Api

migrate-add: ## Create a new migration (usage: make migrate-add NAME=Foo)
	dotnet ef migrations add $(NAME) --project src/Api

clean: ## Remove build artifacts
	dotnet clean
	rm -rf src/web/node_modules src/web/dist

format: ## Auto-fix formatting per .editorconfig
	dotnet format Invoicing.Claude.Code.slnx

format-check: ## Full compliance: nullable warnings + formatting
	dotnet format Invoicing.Claude.Code.slnx --verify-no-changes
	@! dotnet build Invoicing.Claude.Code.slnx --no-restore 2>&1 | grep -E "^\s+warning CS"
	@echo "✓ Full compliance: nullable warnings + formatting"

restore: ## Restore .NET dependencies (in a container, no local SDK needed)
	$(SDK_RUN) dotnet restore Invoicing.Claude.Code.slnx

up: ## Start api + db + aspire-dashboard (detached)
	$(DOCKER_COMPOSE) up --build -d

down: ## Stop containers started via up/run-api
	$(DOCKER_COMPOSE) down

logs: ## Tail logs from all compose services
	$(DOCKER_COMPOSE) logs -f

clean-containers: ## Stop containers and remove volumes (drops local db data)
	$(DOCKER_COMPOSE) down -v
