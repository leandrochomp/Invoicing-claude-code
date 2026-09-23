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

## Logging

### Stack
- `ILogger<T>` for application logging
- OpenTelemetry logging bridge for export
- OTLP exporter to your backend Aspire Dashboard

### Conventions
- Every write handler (`Create*`/`Update*`/`Delete*Handler`, login, register) injects `ILogger<THandler>` and logs:
  - `Information` on success, with the affected entity IDs.
  - `Warning` for each rejected business outcome (not found, conflict, concurrency, over-balance, failed login).
  - `Error` only for failures the handler reports itself; unhandled exceptions are logged by `GlobalExceptionHandler`.
- Read-only queries don't log; request/response telemetry comes from OpenTelemetry tracing.
- Use message templates with PascalCase placeholders (`"Invoice {InvoiceId} deleted"`), never string interpolation.
- Never log passwords, password hashes, or tokens. Don't log the username on a failed login either: it may be a mistyped password.
- Tests assert on logs with `logger.ReceivedLog(LogLevel.X, fragment)` from `tests/InvoicingApi.Tests/TestLogger.cs`.

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

## Minimal API Endpoints

- Endpoints are thin HTTP adapters: bind request, delegate to a handler, map result to `IResult`. No business logic, no repository calls, no `DbContext` in the lambda.
- The handler class is named `XxxHandler`, never `XxxCommand`. A *command* is the message (record); the class that processes it is a **handler**. Even when injected directly (no MediatR), name it `CreateClientHandler` and expose `HandleAsync`.
- Validation runs in a `ValidationFilter<T>` endpoint filter, attached with `.AddEndpointFilter<ValidationFilter<T>>()`. Do not call `IValidator<T>.ValidateAsync` inside the endpoint body — it must be declarative so it cannot be forgotten.
- Write endpoints declare their success status: `Results.Created(...)` (201 + `Location`) for POST-create, with `.Produces<T>(StatusCodes.Status201Created)` and `.ProducesValidationProblem()` for OpenAPI.
- Endpoints are registered via `MapXxxEndpoint(this IEndpointRouteBuilder app)` per feature. Never register endpoints inline in `Program.cs`.

```csharp
app.MapPost("/clients", async (
    CreateClientRequest request,
    CreateClientHandler handler,
    CancellationToken ct) =>
        (await handler.HandleAsync(request, ct)).ToApiResult())
.AddEndpointFilter<ValidationFilter<CreateClientRequest>>()
.RequireAuthorization()
.WithName("CreateClient")
.Produces<ClientSummaryDto>(StatusCodes.Status201Created)
.ProducesValidationProblem();
```

## Clean Code Skill Precedence
When the `clean-code` skill suggests exceptions for error handling, prefer `Result<T>` for business outcomes as documented above. Use exceptions only for programming errors (guards) and infrastructure failures.

## Global exception middleware
Thrown exceptions (guards, infrastructure failures, unexpected bugs) bubble up to a single exception handler. Do NOT wrap every handler in try/catch.
- Implemented via ASP.NET Core's `IExceptionHandler` (`InvoicingApi.Infrastructure.ExceptionHandling.GlobalExceptionHandler`), registered with `AddProblemDetails()` + `AddExceptionHandler<GlobalExceptionHandler>()`, and wired first in the pipeline via `app.UseExceptionHandler()`.
- Unhandled exceptions produce a generic 500 `application/problem+json` response — no exception details are exposed to the client.
