using InvoicingApi.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

public class UpdateInvoiceValidatorTests
{
    private static readonly UpdateInvoiceValidator Validator = new();

    private static UpdateInvoiceRequest ValidRequest() => new(
        ClientId: Guid.NewGuid(),
        IssueDate: DateTimeOffset.UtcNow,
        DueDate: DateTimeOffset.UtcNow.AddDays(30),
        Currency: "USD",
        Notes: null,
        Version: 0,
        Items: [new UpdateInvoiceItemRequest(null, "Widget", 1, 10m, 0, 0)]);

    [Fact]
    public async Task Valid_request_passes()
    {
        var result = await Validator.ValidateAsync(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Negative_version_fails()
    {
        var request = ValidRequest() with { Version = -1 };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Empty_items_fails()
    {
        var request = ValidRequest() with { Items = [] };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Due_date_before_issue_date_fails()
    {
        var request = ValidRequest() with { DueDate = DateTimeOffset.UtcNow.AddDays(-1) };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Notes_longer_than_max_length_fails()
    {
        var request = ValidRequest() with { Notes = new string('a', 4001) };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }
}
