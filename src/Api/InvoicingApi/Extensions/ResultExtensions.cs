using Ardalis.Result;

namespace InvoicingApi.Extensions;

public static class ResultExtensions
{
    public static Microsoft.AspNetCore.Http.IResult ToApiResult<T>(this Result<T> result) =>
        Ardalis.Result.AspNetCore.ResultExtensions.ToMinimalApiResult(result);
}
