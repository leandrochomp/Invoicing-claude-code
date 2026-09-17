# Testing

## Stack
- Framework: xUnit
- Assertions: Shouldly
- Mocking: NSubstitute
- Integration: Testcontainers (not yet set up)

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
