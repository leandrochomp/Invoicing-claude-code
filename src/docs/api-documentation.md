# API Documentation
- OpenAPI via `Microsoft.AspNetCore.OpenApi` at `/openapi/v1.json`.
- Scalar UI at `/scalar/v1` in **all environments**.
- No Swashbuckle. Do not add it.
- Every endpoint: `.WithName()`, `.WithSummary()`, `.Produces<T>()`.
- Internal-only endpoints use `.ExcludeFromDescription()`.
## Invoice lifecycle

Status is never set by the client. It changes only through the action endpoints or is derived.

| From | Action | Endpoint | Otherwise |
|---|---|---|---|
| Draft | edit anything, delete | `PUT /invoices/{id}`, `DELETE /invoices/{id}` | |
| Draft | → Sent (content frozen) | `POST /invoices/{id}/send` `{ version }` | 409 if not Draft or `version` is stale |
| Sent | edit `Notes` only | `PUT /invoices/{id}` | 409 if any other field differs |
| Sent ↔ Paid | derived from payments | payment endpoints (`PaymentStatusUpdater`) | |
| Sent | → Void, only with no payments | `POST /invoices/{id}/void` `{ version }` | 409 if not Sent, has payments, or `version` is stale |
| Paid, Void | terminal: no edits, no delete | | 409 |

- `DELETE` works on Drafts only; any other status returns 409.
- **Overdue is derived, never stored.** A Sent invoice reads as `Overdue` once its due day has passed
  (UTC calendar day, via `TimeProvider`). `GET /invoices/{id}`, `GET /invoices` and `?status=Overdue` share
  the rule in `InvoiceLifecycle`, and `?status=Sent` excludes overdue invoices.
