using Ardalis.Result;
using InvoicingApi.Extensions;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace InvoicingApi.Tests.Extensions;

public class ResultExtensionsTests
{
    [Fact]
    public void Success_result_maps_to_200()
    {
        var result = Result<string>.Success("hello");

        var apiResult = result.ToApiResult();

        var statusResult = apiResult.ShouldBeAssignableTo<IStatusCodeHttpResult>();
        statusResult!.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public void Not_found_result_maps_to_404()
    {
        var result = Result<string>.NotFound();

        var apiResult = result.ToApiResult();

        var statusResult = apiResult.ShouldBeAssignableTo<IStatusCodeHttpResult>();
        statusResult!.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
