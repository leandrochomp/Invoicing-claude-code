# Database

## Primary Keys

All entities use `Guid.CreateVersion7()` for primary keys. Never `Guid.NewGuid()`.

Rationale: v7 GUIDs embed a timestamp, making them time-ordered. This keeps
B-tree index inserts sequential instead of random, reducing page splits and
improving write performance.

## Soft Delete (Default)

All entities use soft delete by default. Never hard-delete rows.

### Implementation

Entities inherit from `SoftDeletableEntity`

## Multi-tenancy

Tenant-owned entities implement `ITenantOwned` and carry a non-null `TenantId`. See `security.md` (Multi-tenancy) for how isolation is enforced.

- Never set `TenantId` in a handler. `InvoicingDbContext` stamps it on insert from the request's tenant.
- Never call a bare `IgnoreQueryFilters()`. Name the filter you mean: `IgnoreQueryFilters([SoftDeleteQueryFilter.Name])`. Only `Features/Admin/` handlers may ignore `TenantQueryFilter.Name`.
- A new tenant-owned entity gets the `"Tenant"` filter automatically by implementing `ITenantOwned`. Also give it composite foreign keys that include `TenantId`, and include `TenantId` in its unique indexes where uniqueness is per tenant (as in `(TenantId, InvoiceNumber)`).
- Adding tenancy made no backfill migration: the dev database holds only test data, so reset it (`make clean-containers`, then `make migrate`) if `AddTenants` fails on existing rows.

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
