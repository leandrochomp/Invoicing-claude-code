using InvoicingApi.Features.Users;
using Shouldly;

namespace InvoicingApi.Tests.Features.Users;

public class RegisterUserRequestValidatorTests
{
    private static readonly RegisterUserRequestValidator Validator = new();

    private static RegisterUserRequest ValidRequest() => new(
        Username: "jane.doe",
        Password: "correct-horse-battery-staple");

    [Fact]
    public void Valid_request_passes()
    {
        var result = Validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_username_fails()
    {
        var request = ValidRequest() with { Username = string.Empty };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RegisterUserRequest.Username));
    }

    [Fact]
    public void Password_shorter_than_eight_characters_fails()
    {
        var request = ValidRequest() with { Password = "short1" };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RegisterUserRequest.Password));
    }

    [Fact]
    public void Password_longer_than_max_length_fails()
    {
        var request = ValidRequest() with { Password = new string('a', 201) };

        var result = Validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RegisterUserRequest.Password));
    }
}
