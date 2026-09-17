## Project engineering guide
Invoicing REST API. Backend: .NET 10 Minimal API + EF Core (Npgsql). Frontend: React + Vite. Keep changes maintainable, tested and consistent with the surrounding code.
New behavior requires a test in `/tests/`. Bug fixes require a regression test.

Add packages via `dotnet add package` or `npm install`. Ask first.

## Repository layout
- `/src/Api` contains dotnet 10 backend webapi.
- `/src/Shared` contains dotnet 10 class library general-purpose helpers that have no application-specific behavior.
- `/tests/Shared.Tests` contains xUnit library.
- `/src/web` contains the React client.

## Commands

Reference `src/docs/commands.md`

## Conventions

### Git & GitHub

Reference `src/docs/github.md`

- Commit style: Conventional Commits — `type(scope): imperative summary`.
  Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `build`.
  Example: `feat(api): add invoice line item validation`
- Subject line: ≤ 72 chars, lowercase, no trailing period.
- One logical change per commit; include regression/behavior tests in the same commit as the change.
- Never commit secrets, connection strings, or generated build artifacts.
- Always commit locally first, then push. Never `--force` to `main`.
- Run `dotnet build` / `npm run build` before committing (per repo constraints).
- All work happens on a feature branch — never commit directly to `main`.
- Branch naming: `type/short-description` (e.g. `feat/invoice-line-validation`, `fix/tax-calc-rounding`, `docs/api-readme`).
  Use the same type as the commit it belongs to; include the issue number when one exists (`feat/12-invoice-validation`).
- One branch per logical unit of work. Branches are short-lived: cut from latest `main`, push, PR, merge, delete.
- Push to a feature branch; open PRs with `gh pr create`. Use `gh` for all GitHub operations — no new GitHub-related dependencies.
- PRs must pass builds and link the issue they close (e.g. `Closes #12`).

### API Documentation

Reference `src/docs/api-documentation.md`

### Backend

Reference `src/docs/backend.md`

### Frontend

Reference `src/docs/frontend.md` 

### Database
- PostgreSQL on localhost:5432.
- Connection string from `ConnectionStrings:Default` in user secrets, never appsettings.json.

## Constraints
- Never commit connection strings, API keys, or secrets.
- Never modify files in `/migrations` by hand.
- Ask before adding new NuGet or npm dependencies.
- Run `dotnet build` after backend changes; `npm run build` after frontend changes.

## Token Discipline
- Ponytail is active. Prefer stdlib and native features over new dependencies.
- Caveman is active. If reading large logs or diffs, summarize before acting.

## Platform
Ubuntu 24.04. Rider IDE. dotnet CLI. Claude Code in Rider's integrated terminal.
- Use `/ide` to surface diffs in Rider.