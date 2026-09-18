using InvoicingApi.Features.Auth;
using Shouldly;

namespace InvoicingApi.Tests.Features.Auth;

public class LoginRequestValidatorTests
{
    private static readonly LoginRequestValidator Validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = Validator.Validate(new LoginRequest("jane.doe", "correct-horse-battery-staple"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_username_fails()
    {
        var result = Validator.Validate(new LoginRequest(string.Empty, "correct-horse-battery-staple"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(LoginRequest.Username));
    }

    [Fact]
    public void Empty_password_fails()
    {
        var result = Validator.Validate(new LoginRequest("jane.doe", string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(LoginRequest.Password));
    }
}
