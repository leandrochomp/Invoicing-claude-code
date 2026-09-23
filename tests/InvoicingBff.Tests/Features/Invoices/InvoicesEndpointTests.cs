using System.Net;
using System.Net.Http.Json;
using InvoicingBff.Features.Invoices;
using InvoicingBff.Tests.TestSupport;
using Shouldly;
using static InvoicingBff.Tests.TestSupport.BffTestClient;

namespace InvoicingBff.Tests.Features.Invoices;

public class InvoicesEndpointTests
{
    private static readonly Guid ClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InvoiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset IssueDate = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private static CreateInvoiceRequest ValidCreateRequest() => new(
        ClientId,
        IssueDate,
        IssueDate.AddDays(30),
        "USD",
        Notes: null,
        [new CreateInvoiceItemRequest("Consulting", 2m, 150m, 0.1m, 0)]);

    [Fact]
    public async Task ListInvoices_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new BffTestFactory(_ => throw new InvalidOperationException("should not call InvoicingApi"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/bff/invoices");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListInvoices_ForwardsFiltersAndTheSessionToken()
    {
        string? receivedQuery = null;
        string? receivedAuthHeader = null;
        using var factory = new BffTestFactory(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/invoices")
            {
                receivedQuery = request.RequestUri.Query;
                receivedAuthHeader = request.Headers.Authorization?.ToString();
                return JsonResponse(HttpStatusCode.OK, """{"items":[],"page":2,"pageSize":10,"totalRecords":0,"totalPages":0}""");
            }

            return JsonResponse(HttpStatusCode.OK, ValidLoginJson);
        });
        using var client = await AuthenticatedAsync(factory);

        var response = await client.GetAsync($"/bff/invoices?clientId={ClientId}&status=Sent&page=2&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        receivedQuery.ShouldBe($"?page=2&pageSize=10&clientId={ClientId}&status=Sent");
        receivedAuthHeader.ShouldBe("Bearer fake-jwt");
    }

    [Fact]
    public async Task ListInvoices_WithoutFilters_SendsOnlyPaging()
    {
        string? receivedQuery = null;
        using var factory = new BffTestFactory(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/invoices")
            {
                receivedQuery = request.RequestUri.Query;
                return JsonResponse(HttpStatusCode.OK, """{"items":[],"page":1,"pageSize":50,"totalRecords":0,"totalPages":0}""");
            }

            return JsonResponse(HttpStatusCode.OK, ValidLoginJson);
        });
        using var client = await AuthenticatedAsync(factory);

        await client.GetAsync("/bff/invoices");

        receivedQuery.ShouldBe("?page=1&pageSize=50");
    }

    [Fact]
    public async Task GetInvoiceById_WhenNotFound_ReturnsNotFound()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath.StartsWith("/invoices/", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.GetAsync($"/bff/invoices/{InvoiceId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateInvoice_WithoutLineItems_ReturnsValidationProblemWithoutCallingTheApi()
    {
        var apiCalls = 0;
        using var factory = new BffTestFactory(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/invoices")
            {
                apiCalls++;
            }

            return JsonResponse(HttpStatusCode.OK, ValidLoginJson);
        });
        using var client = await AuthenticatedAsync(factory);

        var response = await client.PostAsJsonAsync("/bff/invoices", ValidCreateRequest() with { Items = [] });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        apiCalls.ShouldBe(0);
    }

    [Fact]
    public async Task CreateInvoice_WithDueDateBeforeIssueDate_ReturnsValidationProblem()
    {
        using var factory = new BffTestFactory(_ => JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.PostAsJsonAsync("/bff/invoices", ValidCreateRequest() with { DueDate = IssueDate.AddDays(-1) });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateInvoice_WithValidBody_ForwardsToInvoicingApiAndReturnsCreated()
    {
        string? forwardedBody = null;
        using var factory = new BffTestFactory(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/invoices" && request.Method == HttpMethod.Post)
            {
                forwardedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonResponse(HttpStatusCode.Created, $$"""{"id":"{{InvoiceId}}"}""");
            }

            return JsonResponse(HttpStatusCode.OK, ValidLoginJson);
        });
        using var client = await AuthenticatedAsync(factory);

        var response = await client.PostAsJsonAsync("/bff/invoices", ValidCreateRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        forwardedBody.ShouldNotBeNull();
        forwardedBody.ShouldContain("\"currency\":\"USD\"");
        forwardedBody.ShouldContain("\"taxRate\":0.1");
    }

    [Fact]
    public async Task UpdateInvoice_WithUnknownStatus_ReturnsValidationProblem()
    {
        using var factory = new BffTestFactory(_ => JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.PutAsJsonAsync($"/bff/invoices/{InvoiceId}", new UpdateInvoiceRequest(
            ClientId,
            (InvoiceStatus)42,
            IssueDate,
            IssueDate.AddDays(30),
            "USD",
            Notes: null,
            Version: 0,
            [new UpdateInvoiceItemRequest(null, "Consulting", 1m, 100m, 0m, 0)]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateInvoice_WhenStaleUpstream_ReturnsConflict()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == $"/invoices/{InvoiceId}" && request.Method == HttpMethod.Put
            ? JsonResponse(HttpStatusCode.Conflict, """{"title":"The invoice was modified by another request."}""")
            : JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.PutAsJsonAsync($"/bff/invoices/{InvoiceId}", new UpdateInvoiceRequest(
            ClientId,
            InvoiceStatus.Sent,
            IssueDate,
            IssueDate.AddDays(30),
            "USD",
            Notes: null,
            Version: 3,
            [new UpdateInvoiceItemRequest(null, "Consulting", 1m, 100m, 0m, 0)]));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteInvoice_WhenSuccessful_ReturnsNoContent()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == $"/invoices/{InvoiceId}" && request.Method == HttpMethod.Delete
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.DeleteAsync($"/bff/invoices/{InvoiceId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
