using InvoicingApi.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

public class UpdatePaymentValidatorTests
{
    private static readonly UpdatePaymentValidator Validator = new();

    private static UpdatePaymentRequest ValidRequest() => new(
        Amount: 50m,
        PaymentDate: DateTimeOffset.UtcNow,
        Method: PaymentMethod.Card,
        Notes: null,
        Version: 0);

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
    public async Task Undefined_method_fails()
    {
        var request = ValidRequest() with { Method = (PaymentMethod)999 };

        var result = await Validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Negative_version_fails()
    {
        var request = ValidRequest() with { Version = -1 };

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
