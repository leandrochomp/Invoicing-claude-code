.PHONY: build build-api build-web test run-api run-web migrate clean help

build: build-api ## Build backend

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "%-15s %s\n", $$1, $$2}'

build-api: ## Build the backend
	dotnet build src/Api/InvoicingApi/InvoicingApi.csproj

build-web: ## Build the frontend
	cd src/web && npm run build # TODO

test: ## Run all tests
	dotnet test

run-api: ## Run the API locally
	dotnet run --project src/Api/InvoicingApi

run-web: ## Run the React dev server
	cd src/web && npm run dev

migrate: ## Apply EF Core migrations
	dotnet ef database update --project src/Api

migrate-add: ## Create a new migration (usage: make migrate-add NAME=Foo)
	dotnet ef migrations add $(NAME) --project src/Api

clean: ## Remove build artifacts
	dotnet clean
	rm -rf src/web/node_modules src/web/dist