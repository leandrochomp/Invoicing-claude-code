# API Documentation
- OpenAPI via `Microsoft.AspNetCore.OpenApi` at `/openapi/v1.json`.
- Scalar UI at `/scalar/v1` in **all environments**.
- No Swashbuckle. Do not add it.
- Every endpoint: `.WithName()`, `.WithSummary()`, `.Produces<T>()`.
- Internal-only endpoints use `.ExcludeFromDescription()`.