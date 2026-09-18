# Security

## Stack
- **Authentication**: JWT Bearer tokens
- **Password hashing**: BCrypt.Net-Next
- **Authorization**: ASP.NET Core policy-based, driven by role claims (`AdminOnly` policy for admin-gated actions; every other authenticated endpoint just requires a valid token)
- **Rate limiting**: ASP.NET Core's built-in fixed-window limiter (`Microsoft.AspNetCore.RateLimiting`), applied to `/auth/login` and `/auth/register`
- **Transport**: HTTPS enforced via `UseHttpsRedirection` + `UseHsts` (non-Development)

## OWASP Top 10 (2021) coverage

| # | Category | How this codebase addresses it |
|---|----------|---------------------------------|
| A01 | Broken Access Control | Every business endpoint requires a valid JWT (`RequireAuthorization()`); `DELETE /clients/{id}` additionally requires the `AdminOnly` policy. Only `/auth/login`, `/auth/register`, `/health`, and the OpenAPI/Scalar docs routes stay anonymous. |
| A02 | Cryptographic Failures | Passwords are hashed with BCrypt, never stored or logged in plain text. JWTs are signed with HMACSHA256 using a signing key whose minimum length (32 bytes / 256 bits) is enforced at startup. Secrets (`Jwt:SigningKey`, `ConnectionStrings:Default`) live only in user secrets, never in `appsettings.json` or source control. Transport is HTTPS-only. |
| A03 | Injection | All data access goes through EF Core's parameterized LINQ — no raw SQL or string-built queries anywhere in the codebase. Endpoints bind to dedicated request DTOs, never directly to entities, so there's no mass-assignment path either. FluentValidation runs on every request DTO. XSS is not currently an active risk: this repo has no HTML-rendering frontend yet (`/src/Web` is unbuilt scaffolding), so there's no untrusted-content-in-HTML surface today. When the React app is built, it must not use `dangerouslySetInnerHTML`/`innerHTML` on unsanitized data, and should ship a Content-Security-Policy once `index.html` exists. |
| A04 | Insecure Design | Layered error handling — FluentValidation (request shape) → Ardalis.GuardClauses (impossible states) → `Ardalis.Result` (business outcomes) — keeps validation, invariants, and business rules from blurring together. Entities use soft delete instead of hard delete. Login returns a generic `401` on bad credentials rather than revealing whether the username exists. |
| A05 | Security Misconfiguration | `GlobalExceptionHandler` never returns exception messages or stack traces to the client, only a generic `ProblemDetails` body. Every response carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, and `Referrer-Policy: no-referrer`. The `/__test/throw` diagnostic endpoint is compiled into every build but only mapped when `ASPNETCORE_ENVIRONMENT=Testing`. |
| A06 | Vulnerable & Outdated Components | Package versions are centrally managed in `Directory.Packages.props`, and the .NET SDK's built-in NuGet Audit runs on every restore. **Known gap**: there's no CI pipeline yet (no `.github/` directory), so vulnerable-package scanning isn't automated — add a Dependabot config or a `dotnet list package --vulnerable` CI step when CI is set up. |
| A07 | Identification & Authentication Failures | `/auth/login` and `/auth/register` are rate-limited (5 requests/minute per client IP) to slow brute-force and credential-stuffing attempts. JWTs expire after 60 minutes and are validated on issuer, audience, signing key, and lifetime. |
| A08 | Software & Data Integrity Failures | EF Core migrations are checked into source control and reviewed like any other code change — never hand-edited (see root `AGENTS.md`/`CLAUDE.md`). The API doesn't deserialize or execute untrusted data (no dynamic type loading, no `BinaryFormatter`-style deserialization). |
| A09 | Security Logging & Monitoring Failures | OpenTelemetry tracing/logging is wired up for the whole request pipeline (`AddOpenTelemetry()` in `WebApplicationBuilderExtensions.cs`), and unhandled exceptions are logged centrally by `GlobalExceptionHandler`. **Known gap**: there's no security-specific audit trail yet (e.g. logging failed login attempts, role changes, or admin actions as distinct events) — add one if/when compliance or incident-response needs require it. |
| A10 | Server-Side Request Forgery (SSRF) | Not applicable today — the API makes no outbound HTTP requests driven by user-supplied input. Revisit if a future feature adds webhooks, URL fetching, or similar. |

## Known limitations / roadmap
- **No refresh tokens or token revocation.** JWTs are stateless; a compromised token is valid until it expires (60 minutes) — there's no server-side blacklist/allowlist. Logout, password changes, and role changes don't invalidate outstanding tokens.
- **No CORS policy configured.** There's no browser client yet (the React app hasn't been scaffolded, and `src/Web/InvoicingBff` is unmodified `dotnet new web` scaffolding), so there's nothing concrete to scope a policy to. When a browser client is added, configure an explicit origin allow-list — never a wildcard combined with credentials.
- **No Content-Security-Policy.** Same reasoning as CORS: no HTML-rendering frontend exists yet. Add one alongside the React app.
- **No CI-based dependency vulnerability scanning.** The .NET SDK's NuGet Audit runs locally on restore, but nothing enforces it in CI (there is no CI yet).
- **User self-registration has no path to Admin.** `POST /auth/register` always creates a `User`-role account by design; promoting someone to `Admin` is currently a manual database update, not an API operation.
