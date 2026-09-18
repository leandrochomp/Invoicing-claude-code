using InvoicingApi.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

public class CreateInvoiceValidatorTests
{
    private static readonly CreateInvoiceValidator Validator = new();

    private static CreateInvoiceRequest ValidRequest() => new(
        ClientId: Guid.NewGuid(),
        IssueDate: DateTimeOffset.UtcNow,
        DueDate: DateTimeOffset.UtcNow.AddDays(30),
        Currency: "USD",
        Notes: null,
        Items: [new CreateInvoiceItemRequest("Widget", 1, 10m, 0, 0)]);

    [Fact]
    public async Task Valid_request_passes()
    {
        var result = await Validator.ValidateAsync(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Empty_client_id_fails()
    {
        var request = ValidRequest() with { ClientId = Guid.Empty };

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
    public async Task Empty_items_fails()
    {
        var request = ValidRequest() with { Items = [] };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Item_with_zero_quantity_fails()
    {
        var request = ValidRequest() with { Items = [new CreateInvoiceItemRequest("Widget", 0, 10m, 0, 0)] };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Currency_not_three_characters_fails()
    {
        var request = ValidRequest() with { Currency = "US" };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }
}
