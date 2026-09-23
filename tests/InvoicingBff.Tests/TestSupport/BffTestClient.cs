using System.Net;
using System.Net.Http.Json;
using System.Text;
using InvoicingBff.Features.Auth;
using Shouldly;

namespace InvoicingBff.Tests.TestSupport;

public static class BffTestClient
{
    public const string ValidLoginJson = """{"token":"fake-jwt","expiresAt":"2030-01-01T00:00:00Z"}""";

    // Logs in through the BFF (the fake InvoicingApi must answer /auth/login with ValidLoginJson)
    // and returns a client that carries the resulting session cookie.
    public static async Task<HttpClient> AuthenticatedAsync(BffTestFactory factory)
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/bff/login", new LoginRequest("alice", "correct-password"));
        client.DefaultRequestHeaders.Add("Cookie", ExtractSessionCookie(loginResponse));
        return client;
    }

    public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    public static HttpResponseMessage ProblemResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/problem+json"),
    };

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        var cookie = cookies!.First(c => c.StartsWith("InvoicingBff.Auth=", StringComparison.Ordinal));
        return cookie[..cookie.IndexOf(';')];
    }
}
