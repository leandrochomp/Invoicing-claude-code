using InvoicingApi.Infrastructure.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Infrastructure.ExceptionHandling;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task Sets_internal_server_error_status_and_writes_problem_details()
    {
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
        problemDetailsService.TryWriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(true);
        var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        var handler = new GlobalExceptionHandler(problemDetailsService, logger);
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("boom");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        await problemDetailsService.Received(1).TryWriteAsync(Arg.Is<ProblemDetailsContext>(ctx =>
            ctx.HttpContext == httpContext &&
            ctx.Exception == exception &&
            ctx.ProblemDetails.Status == StatusCodes.Status500InternalServerError));
    }
}
