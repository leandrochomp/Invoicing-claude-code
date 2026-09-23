using FluentValidation;
using InvoicingBff.Infrastructure.Validation;
using Shouldly;

namespace InvoicingBff.Tests.Infrastructure.Validation;

public class ContactRuleExtensionsTests
{
    private sealed record Contact(string? Email, string? Phone);

    private sealed class ContactValidator : AbstractValidator<Contact>
    {
        public ContactValidator()
        {
            RuleFor(x => x.Email).ValidEmail();
            RuleFor(x => x.Phone).ValidPhone();
        }
    }

    private static readonly ContactValidator Validator = new();

    [Theory]
    [InlineData("a@b")]
    [InlineData("name@company")]
    [InlineData("name@@company.com")]
    [InlineData("name.@company.com")]
    [InlineData("name@company.c")]
    public void Malformed_email_fails(string email)
    {
        var result = Validator.Validate(new Contact(email, null));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(Contact.Email));
    }

    [Theory]
    [InlineData("billing@acme.test")]
    [InlineData("jane.doe+billing@mail.acme.co.uk")]
    public void Well_formed_email_passes(string email)
    {
        var result = Validator.Validate(new Contact(email, null));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("call me")]
    [InlineData("12345")]
    [InlineData("1234567890123456")]
    [InlineData("61+2 5550 1234")]
    public void Malformed_phone_fails(string phone)
    {
        var result = Validator.Validate(new Contact(null, phone));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(Contact.Phone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("+61 2 5550 1234")]
    [InlineData("(555) 010-0100")]
    public void Missing_or_well_formed_phone_passes(string? phone)
    {
        var result = Validator.Validate(new Contact(null, phone));

        result.IsValid.ShouldBeTrue();
    }
}
