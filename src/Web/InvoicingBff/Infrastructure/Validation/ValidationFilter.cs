using FluentValidation;

namespace InvoicingBff.Infrastructure.Validation;

public sealed class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return await next(context);
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var validationResult = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

        return validationResult.IsValid
            ? await next(context)
            : Results.ValidationProblem(validationResult.ToDictionary());
    }
}
