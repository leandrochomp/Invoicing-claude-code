using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InvoicingApi.Tests;

public static class TestLogger
{
    // ILogger.LogInformation/LogWarning are extension methods over ILogger.Log, so NSubstitute
    // can't assert on them directly; this asserts the underlying Log(...) call instead.
    public static void ReceivedLog<T>(this ILogger<T> logger, LogLevel level, string expectedMessageFragment) =>
        logger.Received(1).Log(
            level,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains(expectedMessageFragment)),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
}
