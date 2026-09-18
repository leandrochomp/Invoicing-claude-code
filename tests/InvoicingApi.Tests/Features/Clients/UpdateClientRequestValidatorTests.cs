using InvoicingApi.Features.Clients;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

public class UpdateClientRequestValidatorTests
{
    private static readonly UpdateClientRequestValidator Validator = new();

    private static UpdateClientRequest ValidRequest() => new(
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
        PreferredCurrency: "USD",
        IsActive: true);

    [Fact]
    public void Valid_request_passes()
    {
        var result = Validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_address_line1_fails()
    {
        var request = ValidRequest() with { AddressLine1 = string.Empty };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateClientRequest.AddressLine1));
    }

    [Fact]
    public void Invalid_email_fails()
    {
        var request = ValidRequest() with { Email = "not-an-email" };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateClientRequest.Email));
    }
}
