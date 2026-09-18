namespace InvoicingBff.Tests.TestSupport;

/// <summary>
/// Stands in for InvoicingApi's HTTP transport so BFF tests never make a real network call.
/// </summary>
public sealed class FakeInvoicingApiHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(respond(request));
}
