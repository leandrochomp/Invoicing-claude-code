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

    [Theory]
    [InlineData("a@b")]
    [InlineData("name@company")]
    [InlineData("name@@company.com")]
    [InlineData("name @company.com")]
    [InlineData("name.@company.com")]
    [InlineData("name@company.c")]
    [InlineData("name@-company.com")]
    public void Malformed_email_fails(string email)
    {
        var request = ValidRequest() with { Email = email };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateClientRequest.Email));
    }

    [Theory]
    [InlineData("jane.doe+billing@mail.acme.co.uk")]
    [InlineData("o'brien@acme.io")]
    public void Well_formed_email_passes(string email)
    {
        var request = ValidRequest() with { Email = email };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("call me")]
    [InlineData("555-01ab")]
    [InlineData("12345")]
    [InlineData("1234567890123456")]
    [InlineData("61+2 5550 1234")]
    public void Malformed_phone_fails(string phone)
    {
        var request = ValidRequest() with { Phone = phone };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateClientRequest.Phone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("+61 2 5550 1234")]
    [InlineData("(555) 010-0100")]
    [InlineData("555.010.0100")]
    public void Missing_or_well_formed_phone_passes(string? phone)
    {
        var request = ValidRequest() with { Phone = phone };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }
}
