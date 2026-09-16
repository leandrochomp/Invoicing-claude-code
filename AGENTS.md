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