using System.Net;
using System.Net.Http.Json;
using InvoicingBff.Features.Clients;
using InvoicingBff.Tests.TestSupport;
using Shouldly;

namespace InvoicingBff.Tests.Features.Clients;

public class ClientsEndpointTests
{
    private const string ClientDetailJson = """
        {"id":"11111111-1111-1111-1111-111111111111","companyName":"Acme Corp","contactName":"Jane Doe",
        "email":"billing@acme.test","phone":"555-0100","addressLine1":"1 Main St","addressLine2":null,
        "city":"Springfield","stateOrRegion":"IL","postalCode":"62701","country":"US",
        "preferredCurrency":"USD","isActive":true}
        """;

    [Fact]
    public async Task ListClients_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new BffTestFactory(_ => throw new InvalidOperationException("should not call InvoicingApi"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/bff/clients");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListClients_ForwardsTheSessionTokenAsABearerHeader()
    {
        string? receivedAuthHeader = null;
        using var factory = new BffTestFactory(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/clients")
            {
                receivedAuthHeader = request.Headers.Authorization?.ToString();
                return BffTestClient.JsonResponse(HttpStatusCode.OK, "[]");
            }

            return BffTestClient.JsonResponse(HttpStatusCode.OK, BffTestClient.ValidLoginJson);
        });
        using var client = await BffTestClient.AuthenticatedAsync(factory);

        var response = await client.GetAsync("/bff/clients");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        receivedAuthHeader.ShouldBe("Bearer fake-jwt");
    }

    [Fact]
    public async Task GetClientById_WhenFound_ReturnsTheClientDetail()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == $"/clients/{id}"
            ? BffTestClient.JsonResponse(HttpStatusCode.OK, ClientDetailJson)
            : BffTestClient.JsonResponse(HttpStatusCode.OK, BffTestClient.ValidLoginJson));
        using var client = await BffTestClient.AuthenticatedAsync(factory);

        var response = await client.GetAsync($"/bff/clients/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClientDetail>();
        body.ShouldNotBeNull();
        body.CompanyName.ShouldBe("Acme Corp");
        body.Email.ShouldBe("billing@acme.test");
    }

    [Fact]
    public async Task GetClientById_WhenNotFound_ReturnsNotFound()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath.StartsWith("/clients/", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : BffTestClient.JsonResponse(HttpStatusCode.OK, BffTestClient.ValidLoginJson));
        using var client = await BffTestClient.AuthenticatedAsync(factory);

        var response = await client.GetAsync($"/bff/clients/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateClient_WhenApiRejectsBody_PassesValidationProblemThrough()
    {
        const string validationProblem = """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Email":["'Email' is not a valid email address."]}}""";
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == "/clients"
            ? BffTestClient.ProblemResponse(HttpStatusCode.BadRequest, validationProblem)
            : BffTestClient.JsonResponse(HttpStatusCode.OK, BffTestClient.ValidLoginJson));
        using var client = await BffTestClient.AuthenticatedAsync(factory);

        var response = await client.PostAsJsonAsync("/bff/clients", new CreateClientRequest(
            CompanyName: "",
            ContactName: null,
            Email: "not-an-email",
            Phone: null,
            AddressLine1: "1 Main St",
            AddressLine2: null,
            City: "Springfield",
            StateOrRegion: "IL",
            PostalCode: "62701",
            Country: "US",
            PreferredCurrency: "USD"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldBe(validationProblem);
    }

    [Fact]
    public async Task CreateClient_WithValidBody_ForwardsToInvoicingApiAndReturnsCreated()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == "/clients"
            && request.Method == HttpMethod.Post
                ? BffTestClient.JsonResponse(HttpStatusCode.Created, """{"id":"11111111-1111-1111-1111-111111111111","companyName":"Acme Corp","email":"billing@acme.test"}""")
                : BffTestClient.JsonResponse(HttpStatusCode.OK, BffTestClient.ValidLoginJson));
        using var client = await BffTestClient.AuthenticatedAsync(factory);

        var response = await client.PostAsJsonAsync("/bff/clients", new CreateClientRequest(
            CompanyName: "Acme Corp",
            ContactName: "Jane Doe",
            Email: "billing@acme.test",
            Phone: "555-0100",
            AddressLine1: "1 Main St",
            AddressLine2: null,
            City: "Springfield",
            StateOrRegion: "IL",
            PostalCode: "62701",
            Country: "US",
            PreferredCurrency: "USD"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateClient_ForwardsToInvoicingApi()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == $"/clients/{id}"
            && request.Method == HttpMethod.Put
                ? BffTestClient.JsonResponse(HttpStatusCode.OK, """{"id":"11111111-1111-1111-1111-111111111111","companyName":"Acme Corp","email":"billing@acme.test"}""")
                : BffTestClient.JsonResponse(HttpStatusCode.OK, BffTestClient.ValidLoginJson));
        using var client = await BffTestClient.AuthenticatedAsync(factory);

        var response = await client.PutAsJsonAsync($"/bff/clients/{id}", new UpdateClientRequest(
            CompanyName: "Acme Corp",
            ContactName: "Jane Doe",
            Email: "billing@acme.test",
            Phone: "555-0100",
            AddressLine1: "1 Main St",
            AddressLine2: null,
            City: "Springfield",
            StateOrRegion: "IL",
            PostalCode: "62701",
            Country: "US",
            PreferredCurrency: "USD",
            IsActive: true));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteClient_WhenForbiddenUpstream_ReturnsForbidden()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath.StartsWith("/clients/", StringComparison.Ordinal)
            && request.Method == HttpMethod.Delete
                ? new HttpResponseMessage(HttpStatusCode.Forbidden)
                : BffTestClient.JsonResponse(HttpStatusCode.OK, BffTestClient.ValidLoginJson));
        using var client = await BffTestClient.AuthenticatedAsync(factory);

        var response = await client.DeleteAsync($"/bff/clients/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteClient_WhenSuccessful_ReturnsNoContent()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath.StartsWith("/clients/", StringComparison.Ordinal)
            && request.Method == HttpMethod.Delete
                ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : BffTestClient.JsonResponse(HttpStatusCode.OK, BffTestClient.ValidLoginJson));
        using var client = await BffTestClient.AuthenticatedAsync(factory);

        var response = await client.DeleteAsync($"/bff/clients/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ListClients_WhenInvoicingApiIsUnreachable_ReturnsServiceUnavailable()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == "/clients"
            ? throw new HttpRequestException("connection refused")
            : BffTestClient.JsonResponse(HttpStatusCode.OK, BffTestClient.ValidLoginJson));
        using var client = await BffTestClient.AuthenticatedAsync(factory);

        var response = await client.GetAsync("/bff/clients");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    private sealed record ClientDetail(Guid Id, string CompanyName, string Email);
}
