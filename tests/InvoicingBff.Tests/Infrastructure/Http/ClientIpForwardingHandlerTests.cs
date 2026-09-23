using System.Net;
using InvoicingBff.Infrastructure.Http;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace InvoicingBff.Tests.Infrastructure.Http;

public class ClientIpForwardingHandlerTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Captured { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Captured = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static ClientIpForwardingHandler CreateHandler(HttpContext? context, HttpMessageHandler inner) =>
        new(new HttpContextAccessor { HttpContext = context }) { InnerHandler = inner };

    [Fact]
    public async Task Stamps_client_ip_as_forwarded_for()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        using var inner = new CapturingHandler();
        using var handler = CreateHandler(context, inner);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test/auth/login");

        await invoker.SendAsync(request, CancellationToken.None);

        inner.Captured.ShouldNotBeNull();
        inner.Captured.Headers.GetValues("X-Forwarded-For").ShouldBe(["203.0.113.7"]);
    }

    [Fact]
    public async Task Replaces_any_inbound_forwarded_for_so_the_client_cannot_spoof_it()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        using var inner = new CapturingHandler();
        using var handler = CreateHandler(context, inner);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test/auth/login");
        request.Headers.Add("X-Forwarded-For", "10.0.0.1");

        await invoker.SendAsync(request, CancellationToken.None);

        inner.Captured!.Headers.GetValues("X-Forwarded-For").ShouldBe(["203.0.113.7"]);
    }

    [Fact]
    public async Task Adds_no_header_when_there_is_no_ambient_request()
    {
        using var inner = new CapturingHandler();
        using var handler = CreateHandler(context: null, inner);
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test/auth/login");

        await invoker.SendAsync(request, CancellationToken.None);

        inner.Captured!.Headers.Contains("X-Forwarded-For").ShouldBeFalse();
    }
}
