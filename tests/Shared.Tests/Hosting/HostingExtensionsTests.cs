using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Hosting;
using Shouldly;

namespace Shared.Tests.Hosting;

public class HostingExtensionsTests
{
    [Fact]
    public async Task UseSecurityHeaders_SetsHeadersAndCallsNext()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        var nextCalled = false;
        app.UseSecurityHeaders();
        app.Run(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        await app.Build()(context);

        nextCalled.ShouldBeTrue();
        context.Response.Headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().ShouldBe("DENY");
        context.Response.Headers["Referrer-Policy"].ToString().ShouldBe("no-referrer");
    }

    [Fact]
    public void AddTrustedForwardedHeaders_TrustsASingleXForwardedForHop()
    {
        var options = new ServiceCollection()
            .AddTrustedForwardedHeaders()
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        options.ForwardedHeaders.ShouldBe(ForwardedHeaders.XForwardedFor);
        options.ForwardLimit.ShouldBe(1);
    }

    [Fact]
    public void AddAuthRateLimiter_RejectsWithTooManyRequests()
    {
        var options = new ServiceCollection()
            .AddAuthRateLimiter()
            .BuildServiceProvider()
            .GetRequiredService<IOptions<RateLimiterOptions>>()
            .Value;

        options.RejectionStatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
    }
}
