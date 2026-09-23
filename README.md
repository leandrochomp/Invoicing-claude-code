# Invoicing-claude-code

Invoicing REST API with a React client.

* C# 14.0 / .NET 10 Minimal API + EF Core (Npgsql) — `src/Api/InvoicingApi`
* BFF (Backend-For-Frontend) that brokers cookie auth for the React client — `src/InvoicingBff/InvoicingBff`
* React + Vite client — `src/web`
* PostgreSQL 17 (Docker)
* Vertical Slice Architecture

# Prerequisites

* Docker with the compose plugin
* .NET 10 SDK and the EF Core CLI (`dotnet tool install --global dotnet-ef`) — for migrations and `make run-bff`
* Node.js 24+ — for the React client

# Architecture

```
browser ──► vite dev server :5173 ──/bff/*──► BFF :7180 ──► API :7073 ──► Postgres :5432
                                                               │
                                                               └─ OpenTelemetry ──► Aspire dashboard :18888
```

The React client never calls the API directly: vite proxies `/bff/*` to the BFF, which holds the
session cookie and forwards the user's JWT to the API.

Every HTTP hop uses HTTPS with the ASP.NET Core development certificate, including BFF → API
inside Docker (the api container is reachable as `api.dev.internal`, which the certificate covers).

# Quick start

## 1. Set up the HTTPS dev certificate (once per machine)

```bash
make certs
```

This trusts the ASP.NET Core dev certificate (`dotnet dev-certs https --trust`) and exports it as PEM
to `~/.aspnet/https/`, where docker compose mounts it and Vite loads it. The exported key never
lives in the repo. `make up`/`up-api`/`run-web` export it automatically if it's missing, but only
`make certs` trusts it.

On Linux, `--trust` covers the .NET/OpenSSL side and, when `certutil` is installed
(`sudo apt install libnss3-tools`), Chromium- and Firefox-based browsers. If your browser still
warns about the certificate, import `~/.aspnet/https/invoicing.pem` into its certificate authorities.

## 2. (Optional) configure local settings

```bash
cp .env.example .env   # override Postgres credentials / dev JWT settings; defaults work as-is
```

## 3. Create the database schema (first run, or after `make clean-containers`)

Migrations are **not** applied automatically on startup. Point the EF CLI at the compose database
via user secrets (once per machine), then apply them:

```bash
make up-api        # starts Postgres (and the api)
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=invoicing;Username=postgres;Password=postgres" \
  --project src/Api/InvoicingApi
make migrate
```

Use the credentials from your `.env` if you changed them.

## 4a. Frontend + backend + database

```bash
make dev
```

`make dev` = `make up` (Postgres + API + BFF + Aspire dashboard, detached) followed by the React
dev server in the foreground (installs `node_modules` on first run). Open:

| What              | URL                                |
|-------------------|------------------------------------|
| React app         | https://localhost:5173             |
| BFF               | https://localhost:7180             |
| API + Scalar docs | https://localhost:7073/scalar/v1   |
| Aspire dashboard  | http://localhost:18888             |

`Ctrl+C` stops only the React dev server; `make down` stops the containers.
To run the pieces separately: `make up`, then `make run-web` in another terminal.

## 4b. Backend (with Scalar) + database only

```bash
make up-api
```

Starts Postgres, the API and the Aspire dashboard. Explore and call the API from Scalar at
https://localhost:7073/scalar/v1 (OpenAPI document: https://localhost:7073/openapi/v1.json).

To get a token for authorized endpoints, call `POST /auth/register` and then `POST /auth/login`,
and paste the returned token into Scalar's bearer auth.

## Stopping / resetting

```bash
make down               # stop containers, keep data
make clean-containers   # stop containers and delete the database volume (re-run `make migrate` after)
make logs               # tail logs from all containers
```

# Running services outside Docker (debugging in Rider)

* **API**: run the `https` launch profile (https://localhost:7073/scalar/v1). It reads
  `ConnectionStrings:Default` and `Jwt:SigningKey`/`Jwt:Issuer`/`Jwt:Audience` from user secrets.
  Stop the api container first (`docker compose stop api`) — both use port 7073.
* **BFF**: `make run-bff` runs it on https://localhost:7180 against https://localhost:7073, which is
  either the docker API or the API's Rider `https` profile (same port). Start the backend with
  `make up-api` so the BFF container doesn't hold port 7180. Override with `make run-bff API_URL=...`.

# Make targets

Run `make help` for the full list. Most used:

| Target                       | Description                                              |
|------------------------------|----------------------------------------------------------|
| `make certs`                 | Trust + export the HTTPS dev certificate                 |
| `make dev`                   | Full stack + React dev server                            |
| `make up`                    | Postgres + API + BFF + Aspire dashboard (detached)       |
| `make up-api`                | Postgres + API + Aspire dashboard (detached)             |
| `make run-web`               | React dev server                                         |
| `make run-bff`               | BFF via `dotnet run` against `API_URL`                   |
| `make down`                  | Stop containers                                          |
| `make migrate`               | Apply EF Core migrations                                 |
| `make migrate-add NAME=Foo`  | Create a new migration                                   |
| `make test` / `make test-web`| Backend tests (in SDK container) / frontend tests        |
| `make build` / `make build-web` | Build backend / frontend                              |

# Environment variables (`.env`)

Read by docker compose; see `.env.example`.

| Variable            | Description                           | Default                                       |
|---------------------|---------------------------------------|-----------------------------------------------|
| `POSTGRES_USER`     | Database user                         | `postgres`                                    |
| `POSTGRES_PASSWORD` | Database password                     | `postgres`                                    |
| `POSTGRES_DB`       | Database name                         | `invoicing`                                   |
| `POSTGRES_PORT`     | Host port Postgres is published on    | `5432`                                        |
| `JWT_SIGNING_KEY`   | Dev-only JWT signing key (≥ 32 bytes) | `docker-compose-dev-only-signing-key-32b-min` |
| `JWT_ISSUER`        | JWT issuer                            | `InvoicingApi.Docker`                         |
| `JWT_AUDIENCE`      | JWT audience                          | `InvoicingApi.Docker`                         |

`.env` is git-ignored — never commit it. These defaults are for local development only.

# Database migrations

The EF CLI reads the connection string from the API's user secrets (`ConnectionStrings:Default`).

```bash
make migrate-add NAME=AddSomething   # create a migration in src/Api/InvoicingApi/Migrations
make migrate                         # apply pending migrations

# generate a SQL script (optional)
dotnet ef migrations script --project src/Api/InvoicingApi

# remove the last, unapplied migration
dotnet ef migrations remove --project src/Api/InvoicingApi
```

Never edit files in `Migrations/` by hand.
