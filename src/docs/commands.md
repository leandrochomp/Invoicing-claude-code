# Commands

Run from repo root unless noted.

Prefer `make` targets over raw commands.

- `make build` — build solution
- `make test` — run backend tests
- `make test-web` — run frontend tests (Vitest)
- `make run-api` — run the api, its db and the Aspire dashboard via docker compose
- `make run-bff` — run the InvoicingBff locally on https://localhost:7180 against `API_URL` (default `https://localhost:7073`, the `up-api` container)
- `make run-web` — run React dev server (proxies `/bff/*` to InvoicingBff in dev, see frontend.md)
- `make migrate` — apply EF migrations
- `make migrate-add NAME=Foo` — create migration
- `make certs` — trust the ASP.NET Core dev certificate and export it to `~/.aspnet/https` (HTTPS for docker + vite)
- `make dev` — `make up` + React dev server (full stack, https://localhost:5173)
- `make up` — start db + api + bff + aspire-dashboard (detached)
- `make up-api` — start db + api + aspire-dashboard only (detached; Scalar at https://localhost:7073/scalar/v1)
- `make down` — stop containers
- `make logs` — tail logs from all compose services
- `make clean` — remove build artifacts (dotnet clean + frontend build output)
- `make clean-containers` — stop containers and remove volumes (drops local db data)

`test` and `restore` run inside the .NET SDK container — no local SDK install required.
Copy `.env.example` to `.env` to override the default local Postgres credentials.
The Aspire dashboard (OpenTelemetry traces/metrics/logs) is at http://localhost:18888 once `make up`/`run-api` is running.

Direct commands (if Makefile is unavailable):
- `dotnet build`, `dotnet test`, `dotnet run --project src/Api/InvoicingApi`