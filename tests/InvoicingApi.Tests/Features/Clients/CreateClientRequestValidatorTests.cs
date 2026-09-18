using InvoicingApi.Features.Clients;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

public class CreateClientRequestValidatorTests
{
    private static readonly CreateClientRequestValidator Validator = new();

    private static CreateClientRequest ValidRequest() => new(
        CompanyName: "Acme Corp",
        ContactName: "Jane Doe",
        Email: "billing@acme.test",
        Phone: "555-0100",
        AddressLine1: "1 Main St",
        AddressLine2: null,
        City: "Springfield",
        StateOrRegion: "IL",
        PostalCode: "62701",
        Country: "US",
        PreferredCurrency: "USD");

    [Fact]
    public void Valid_request_passes()
    {
        var result = Validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_company_name_fails()
    {
        var request = ValidRequest() with { CompanyName = string.Empty };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateClientRequest.CompanyName));
    }

    [Fact]
    public void Invalid_email_fails()
    {
        var request = ValidRequest() with { Email = "not-an-email" };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateClientRequest.Email));
    }

    [Fact]
    public void Preferred_currency_must_be_exactly_three_characters()
    {
        var request = ValidRequest() with { PreferredCurrency = "US" };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateClientRequest.PreferredCurrency));
    }
}
