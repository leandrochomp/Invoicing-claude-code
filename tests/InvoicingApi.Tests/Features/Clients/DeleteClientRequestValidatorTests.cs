using InvoicingApi.Features.Clients;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

public class DeleteClientRequestValidatorTests
{
    private static readonly DeleteClientRequestValidator Validator = new();

    [Fact]
    public void Valid_deleted_by_passes()
    {
        var result = Validator.Validate(new DeleteClientRequest(Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_deleted_by_fails()
    {
        var result = Validator.Validate(new DeleteClientRequest(Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DeleteClientRequest.DeletedBy));
    }
}
