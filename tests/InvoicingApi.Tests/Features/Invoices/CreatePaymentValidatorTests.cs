using InvoicingApi.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

public class CreatePaymentValidatorTests
{
    private static readonly CreatePaymentValidator Validator = new();

    private static CreatePaymentRequest ValidRequest() => new(
        Amount: 50m,
        PaymentDate: DateTimeOffset.UtcNow,
        Method: PaymentMethod.Card,
        Notes: null);

    [Fact]
    public async Task Valid_request_passes()
    {
        var result = await Validator.ValidateAsync(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Zero_amount_fails()
    {
        var request = ValidRequest() with { Amount = 0m };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Negative_amount_fails()
    {
        var request = ValidRequest() with { Amount = -10m };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Undefined_method_fails()
    {
        var request = ValidRequest() with { Method = (PaymentMethod)999 };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Notes_longer_than_max_length_fails()
    {
        var request = ValidRequest() with { Notes = new string('a', 501) };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }
}
