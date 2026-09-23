# Testing

## Stack
- Framework: xUnit
- Assertions: Shouldly
- Mocking: NSubstitute
- Integration: Testcontainers (required for all database-backed tests)
- Browser automation: `playwright-cli` skill (see "Browser automation" below)

## Rules

### Never use `Assert.*`
xUnit's `Assert` class is banned. Use Shouldly for all assertions.

### Never use Moq or FluentAssertions
- Moq: replaced by NSubstitute.
- FluentAssertions: replaced by Shouldly (licensing change in 2025).

### Every test file
- `Shouldly` and `NSubstitute` are the only assertion/mocking imports.
- If you find yourself writing `using Xunit;` for anything other than `[Fact]`/`[Theory]`, stop.

## Shouldly patterns

```csharp
// Equality
invoice.Amount.ShouldBe(150.00m);
invoice.CustomerName.ShouldBe("Acme Corp");

// Null / empty
result.ShouldNotBeNull();
invoice.CustomerName.ShouldNotBeNullOrWhiteSpace();

// Booleans
result.IsValid.ShouldBeFalse();
result.IsValid.ShouldBeTrue();

// Collections
invoices.ShouldContain(i => i.Id == expectedId);
invoices.Count.ShouldBe(3);

// Exceptions
Should.Throw<ValidationException>(() => handler.Handle(request));

// Async
await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(request));

// Decimal precision (important for invoicing)
invoice.Total.ShouldBe(1234.56m);
```

### Never use SQLite (or any in-memory database)
- **SQLite is banned**, including `Microsoft.Data.Sqlite`, `Microsoft.EntityFrameworkCore.Sqlite`, and in-memory SQLite (`:memory:`).
- **EF Core InMemory provider is banned** (`Microsoft.EntityFrameworkCore.InMemory`).
- Any test that touches a database **must** use Testcontainers against the same engine as production (SQL Server, PostgreSQL, etc.).
- Rationale: SQLite behaves differently from production databases (types, collations, constraints, transactions, concurrency, SQL dialect). Bugs slip through.

### Never mock `DbContext` or `IQueryable`
- Use Testcontainers with the real provider. Mocking EF Core hides translation bugs.

## Testcontainers

- One container per test *collection*, not per test, unless isolation demands otherwise.
- Use `IAsyncLifetime` (or `WebApplicationFactory` fixtures) to start/stop containers.
- Apply migrations against the container at startup — never `EnsureCreated()` for integration tests if migrations exist.
- Never commit connection strings; get them from the running container.
- Guard on Docker availability: if Docker isn't running, fail loudly — do not silently fall back to SQLite.

```csharp
// Typical pattern
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public string ConnectionString => _db.GetConnectionString();

    public Task InitializeAsync() => _db.StartAsync();
    public Task DisposeAsync() => _db.DisposeAsync();
}
```

## Browser automation

Use the `playwright-cli` skill for all browser interaction. Do not use Playwright MCP — the skill loads lazily, MCP taxes every turn.

### Rules
- Never point the skill at production. Local, staging, or ephemeral only.
- Never commit credentials, cookies, auth state, screenshots, or traces.
- Never use `waitForTimeout`. Rely on web-first assertions and auto-waiting.
- If a session surfaces a bug, encode it as a Playwright test — don't leave it in chat history.

### Playwright (the framework)
- Keep Playwright E2E tests in a separate project from xUnit tests.
- In Playwright files, use Playwright's `expect` — not Shouldly. The Shouldly rule governs xUnit projects.
- Moq, FluentAssertions, and SQLite bans apply here too.
- Prefer `getByRole` / `getByLabel` / `getByTestId`. CSS/XPath is a last resort.
- Database-backed E2E tests use Testcontainers, same as integration tests.

### E2E project (`tests/e2e`)
- Standalone npm project with `@playwright/test`; not part of the .NET solution or `src/web`.
- Runs against the local dev stack: start `make dev`, then `make test-e2e`.
- Credentials come from `E2E_USERNAME` / `E2E_PASSWORD`; `auth.setup.ts` signs in once and saves
  the session to `tests/e2e/.auth/` (gitignored) for the other specs.
- Specs that create data use unique names, since they write to the dev database.
