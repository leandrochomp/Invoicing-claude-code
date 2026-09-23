.PHONY: build build-api build-web test test-web run-api run-bff run-web migrate migrate-add clean help up up-api dev certs down logs restore clean-containers format format-check

DOCKER_COMPOSE := docker compose
SDK_IMAGE := mcr.microsoft.com/dotnet/sdk:10.0
SDK_RUN := docker run --rm -v $(CURDIR):/src -w /src $(SDK_IMAGE)
API_PROJECT := src/Api/InvoicingApi
# Where a locally run BFF (make run-bff) finds the api; the docker api and Rider's https profile share this port.
API_URL ?= https://localhost:7073
# ASP.NET Core dev certificate exported as PEM; mounted by docker compose and loaded by vite.
CERT_DIR := $(HOME)/.aspnet/https
DEV_CERT := $(CERT_DIR)/invoicing.pem

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

run-bff: ## Run the BFF locally against API_URL (default: the up-api container)
	SSL_CERT_DIR=/etc/ssl/certs:$(CERT_DIR) InvoicingApi__BaseUrl=$(API_URL) dotnet run --project src/InvoicingBff/InvoicingBff --launch-profile https

run-web: src/web/node_modules $(DEV_CERT) ## Run the React dev server (https://localhost:5173)
	cd src/web && npm run dev

src/web/node_modules: src/web/package-lock.json
	cd src/web && npm ci

migrate: ## Apply EF Core migrations
	dotnet ef database update --project $(API_PROJECT)

migrate-add: ## Create a new migration (usage: make migrate-add NAME=Foo)
	dotnet ef migrations add $(NAME) --project $(API_PROJECT)

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

up: $(DEV_CERT) ## Start db + api + bff + aspire-dashboard (detached)
	$(DOCKER_COMPOSE) up --build -d

up-api: $(DEV_CERT) ## Start db + api + aspire-dashboard only (detached; Scalar at /scalar/v1)
	$(DOCKER_COMPOSE) up --build -d db api aspire-dashboard

dev: up run-web ## Start the full stack, then the React dev server in the foreground

certs: ## Trust the ASP.NET Core dev certificate and export it as PEM for docker + vite
	dotnet dev-certs https --trust
	mkdir -p $(CERT_DIR)
	dotnet dev-certs https --export-path $(DEV_CERT) --format PEM --no-password

$(DEV_CERT):
	mkdir -p $(CERT_DIR)
	dotnet dev-certs https --export-path $(DEV_CERT) --format PEM --no-password

down: ## Stop containers started via up/up-api/run-api
	$(DOCKER_COMPOSE) down

logs: ## Tail logs from all compose services
	$(DOCKER_COMPOSE) logs -f

clean-containers: ## Stop containers and remove volumes (drops local db data)
	$(DOCKER_COMPOSE) down -v
