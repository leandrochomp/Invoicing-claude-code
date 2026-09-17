# Backend

- Vertical Slice Architecture. Features live under `Features/<Name>/`, one file per slice.
- Minimal API endpoints, no controllers.
- Entities inherit `Entity` (Id: Guid, Version: int for optimistic concurrency).
- DTOs are separate from entities. Never return entities directly.
- EF Core configurations in `IEntityTypeConfiguration<T>`, not attributes.
- Money stored as `decimal(18,2)`. Never float/double.
- Dates stored as UTC. `DateTimeOffset` for invoice issue dates.
- `.editorconfig` at repo root defines C# style. Based on dotnet/runtime's config.
- Nullable warnings (CS8600–CS8629) are errors via `.editorconfig`. `make check` must pass before considering any task done.
- Never "fix" a nullable error with `!` (null-forgiving) unless a comment justifies why the value can't be null. Prefer guard clauses.
- Never add suppressions (`#pragma`, `WarningsNotAsErrors`) without explicit user approval.
- Run `make format-check` or `dotnet format Invoicing.Claude.Code.slnx --verify-no-changes` to check.

## Error Handling

Three layers, each with a distinct purpose. ProblemDetails (RFC 9457) is the **wire format** for all HTTP errors.

### 1. Request Validation — FluentValidation
Validates DTO shape at the API boundary. Runs before the handler does work.
FluentValidation answers: "Is this request well-formed?"
Returns `Results.ValidationProblem` on failure.
- One `AbstractValidator<T>` per request type, defined inside the slice file.
- Registered via assembly scan, not per-validator.
- Invoked explicitly in the endpoint handler so the flow is visible.
- `FluentValidation.DependencyInjectionExtensions`

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

### 2. Guard Clauses — Ardalis.GuardClauses
Protects internal contracts. Throws on programming errors.
Use for impossible states, not business rules.

```csharp
Guard.Against.Null(request);
Guard.Against.NegativeOrZero(amount);
Guard.Against.NullOrWhiteSpace(customerName);
```

### 3. Business Outcomes — Ardalis.Result
Signals expected, nameable business-rule outcomes from service/application-layer methods (not found, cannot perform this transition, etc.) — distinct from request-shape validation and impossible-state guards.
- Service/application-layer methods return `Ardalis.Result.Result<T>` instead of throwing or returning null for these cases.
- Endpoint handlers map the result to HTTP via the `.ToApiResult()` extension (`InvoicingApi.Extensions.ResultExtensions`), which wraps `Ardalis.Result.AspNetCore`'s `ToMinimalApiResult()`.

```csharp
public async Task<Result<ClientSummaryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
{
    var client = await repository.GetByIdAsync(id, cancellationToken);
    return client is null ? Result<ClientSummaryDto>.NotFound() : new ClientSummaryDto(...);
}
```

## Global exception middleware
Thrown exceptions (guards, infrastructure failures, unexpected bugs) bubble up to a single exception handler. Do NOT wrap every handler in try/catch.
- Implemented via ASP.NET Core's `IExceptionHandler` (`InvoicingApi.Infrastructure.ExceptionHandling.GlobalExceptionHandler`), registered with `AddProblemDetails()` + `AddExceptionHandler<GlobalExceptionHandler>()`, and wired first in the pipeline via `app.UseExceptionHandler()`.
- Unhandled exceptions produce a generic 500 `application/problem+json` response — no exception details are exposed to the client.
