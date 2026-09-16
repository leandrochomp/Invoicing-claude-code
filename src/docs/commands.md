# Commands

Run from repo root unless noted.

Prefer `make` targets over raw commands.

- `make build` — build solution
- `make test` — run tests
- `make run-api` — run API locally
- `make run-web` — run React dev server
- `make migrate` — apply EF migrations
- `make migrate-add NAME=Foo` — create migration

Direct commands (if Makefile is unavailable):
- `dotnet build`, `dotnet test`, `dotnet run --project src/Api`