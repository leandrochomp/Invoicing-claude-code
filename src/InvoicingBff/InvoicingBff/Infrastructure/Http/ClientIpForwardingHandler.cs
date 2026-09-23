using Microsoft.AspNetCore.Http;

namespace InvoicingBff.Infrastructure.Http;

// Stamps the browser's IP onto outgoing InvoicingApi calls as X-Forwarded-For. The API sees every
// request coming from the BFF, so without this its rate limiter would bucket all users into one
// window; forwarding the real client IP lets the API throttle brute-force attempts per caller.
public class ClientIpForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string ForwardedForHeader = "X-Forwarded-For";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var remoteIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
        if (remoteIp is not null)
        {
            // Replace rather than append: the BFF is the trust boundary and originates this call,
            // so any inbound X-Forwarded-For must not be relayed on to the API.
            request.Headers.Remove(ForwardedForHeader);
            request.Headers.Add(ForwardedForHeader, remoteIp.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
