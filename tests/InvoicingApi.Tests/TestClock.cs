using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InvoicingApi.Tests;

public static class TestClock
{
    // A partial substitute: only the wall-clock time is pinned, so timers and timestamps used by the
    // rest of the host (JWT validation, rate limiting) keep working. Pin it near the real time so test
    // tokens stay valid.
    public static TimeProvider At(DateTimeOffset now)
    {
        var clock = Substitute.ForPartsOf<TimeProvider>();
        clock.GetUtcNow().Returns(now);
        return clock;
    }

    public static void Apply(IWebHostBuilder builder, DateTimeOffset now) =>
        builder.ConfigureTestServices(services => services.AddSingleton(At(now)));
}
