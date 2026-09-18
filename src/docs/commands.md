# Commands

Run from repo root unless noted.

Prefer `make` targets over raw commands.

- `make build` — build solution
- `make test` — run backend tests
- `make test-web` — run frontend tests (Vitest)
- `make run-api` — run the api, its db and the Aspire dashboard via docker compose
- `make run-bff` — run the InvoicingBff (needs `run-api` running alongside it)
- `make run-web` — run React dev server (proxies `/bff/*` to InvoicingBff in dev, see frontend.md)
- `make migrate` — apply EF migrations
- `make migrate-add NAME=Foo` — create migration
- `make up` / `make down` — start/stop api + db + aspire-dashboard (detached)
- `make logs` — tail logs from all compose services
- `make clean` — remove build artifacts (dotnet clean + frontend build output)
- `make clean-containers` — stop containers and remove volumes (drops local db data)

`test` and `restore` run inside the .NET SDK container — no local SDK install required.
Copy `.env.example` to `.env` to override the default local Postgres credentials.
The Aspire dashboard (OpenTelemetry traces/metrics/logs) is at http://localhost:18888 once `make up`/`run-api` is running.

Direct commands (if Makefile is unavailable):
- `dotnet build`, `dotnet test`, `dotnet run --project src/Api`