# Backend
- Vertical Slice Architecture. Features live under `Features/<Name>/`, one file per slice.
- Minimal API endpoints, no controllers.
- Entities inherit `Entity` (Id: Guid, Version: int for optimistic concurrency).
- DTOs are separate from entities. Never return entities directly.
- EF Core configurations in `IEntityTypeConfiguration<T>`, not attributes.
- Money stored as `decimal(18,2)`. Never float/double.
- Dates stored as UTC. `DateTimeOffset` for invoice issue dates.
- `.editorconfig` at repo root defines C# style. Based on dotnet/runtime's config.
- Run `make format-check` or `dotnet format Invoicing.Claude.Code.slnx --verify-no-changes` to check.