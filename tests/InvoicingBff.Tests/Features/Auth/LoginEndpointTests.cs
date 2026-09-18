using System.Net;
using System.Net.Http.Json;
using InvoicingBff.Features.Auth;
using InvoicingBff.Tests.TestSupport;
using Shouldly;

namespace InvoicingBff.Tests.Features.Auth;

public class LoginEndpointTests
{
    private const string ValidLoginJson = """{"token":"fake-jwt","expiresAt":"2030-01-01T00:00:00Z"}""";

    [Fact]
    public async Task Login_WithValidCredentials_SetsSessionCookieAndReturnsUsername()
    {
        using var factory = new BffTestFactory(_ => JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/bff/login", new LoginRequest("alice", "correct-password"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.ShouldNotBeNull();
        body.Username.ShouldBe("alice");
        response.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        cookies!.ShouldContain(c => c.StartsWith("InvoicingBff.Auth=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        using var factory = new BffTestFactory(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/bff/login", new LoginRequest("alice", "wrong-password"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WhenInvoicingApiIsUnreachable_ReturnsServiceUnavailable()
    {
        using var factory = new BffTestFactory(_ => throw new HttpRequestException("connection refused"));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/bff/login", new LoginRequest("alice", "correct-password"));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Login_WithEmptyUsername_ReturnsValidationProblem()
    {
        using var factory = new BffTestFactory(_ => JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/bff/login", new LoginRequest("", "correct-password"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Session_WithoutCookie_ReturnsUnauthorized()
    {
        using var factory = new BffTestFactory(_ => JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/bff/session");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_AfterLogin_ReturnsUsername()
    {
        using var factory = new BffTestFactory(_ => JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/bff/login", new LoginRequest("alice", "correct-password"));
        client.DefaultRequestHeaders.Add("Cookie", ExtractSessionCookie(loginResponse));

        var sessionResponse = await client.GetAsync("/bff/session");

        sessionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var session = await sessionResponse.Content.ReadFromJsonAsync<SessionResponse>();
        session.ShouldNotBeNull();
        session.Username.ShouldBe("alice");
    }

    [Fact]
    public async Task Logout_ClearsTheSession()
    {
        using var factory = new BffTestFactory(_ => JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/bff/login", new LoginRequest("alice", "correct-password"));
        client.DefaultRequestHeaders.Add("Cookie", ExtractSessionCookie(loginResponse));

        var logoutResponse = await client.PostAsync("/bff/logout", content: null);
        var logoutCookie = ExtractSessionCookie(logoutResponse);
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", logoutCookie);

        var sessionResponse = await client.GetAsync("/bff/session");

        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        sessionResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
    };

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        var cookie = cookies!.First(c => c.StartsWith("InvoicingBff.Auth=", StringComparison.Ordinal));
        return cookie[..cookie.IndexOf(';')];
    }
}
