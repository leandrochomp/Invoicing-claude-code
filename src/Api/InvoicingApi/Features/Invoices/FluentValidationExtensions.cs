using Ardalis.Result;
using FluentValidation.Results;

namespace InvoicingApi.Features.Invoices;

internal static class FluentValidationExtensions
{
    public static IEnumerable<ValidationError> ToValidationErrors(this ValidationResult validationResult) =>
        validationResult.Errors.Select(e => new ValidationError
        {
            Identifier = e.PropertyName,
            ErrorMessage = e.ErrorMessage,
        });
}
