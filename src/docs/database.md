# Database

## Stack
[PostgreSQL 17](postgres:17-alpine) running in Docker. No local Postgres install.

## Container
Defined in `compose.yaml` at repo root. Service name: `postgres`.

Start: `docker compose up -d postgres`
Stop: `docker compose down`
Logs: `docker compose logs -f postgres`
Shell into it: `docker compose exec postgres psql -U postgres -d invoicing`

## Connection
- Host: `localhost` (from host machine) / `postgres` (from other containers)
- Port: `5432`
- Database: `invoicing`
- User: `postgres`
- Password: from `.env` (see `.env.template`)

Connection string format:
`Host=localhost;Port=5432;Database=invoicing;Username=postgres;Password=<from .env>`

Stored in user secrets as `ConnectionStrings:Default`. Never in `appsettings.json`.

## Volume
Named volume `postgres_data` persists between `docker compose down` runs.

Reset the database (destroys all data):
