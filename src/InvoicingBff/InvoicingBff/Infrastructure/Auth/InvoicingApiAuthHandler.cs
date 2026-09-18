using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace InvoicingBff.Infrastructure.Auth;

// Attaches the InvoicingApi JWT (held server-side in the auth cookie's AccessToken claim)
// to outgoing proxied requests, so the browser never sees or sends it.
public class InvoicingApiAuthHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = httpContextAccessor.HttpContext?.User.FindFirst(BffClaimTypes.AccessToken)?.Value;
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
