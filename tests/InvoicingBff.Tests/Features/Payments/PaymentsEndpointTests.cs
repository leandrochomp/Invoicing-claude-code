using System.Net;
using System.Net.Http.Json;
using InvoicingBff.Features.Payments;
using InvoicingBff.Tests.TestSupport;
using Shouldly;
using static InvoicingBff.Tests.TestSupport.BffTestClient;

namespace InvoicingBff.Tests.Features.Payments;

public class PaymentsEndpointTests
{
    private static readonly Guid InvoiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PaymentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset PaidOn = new(2026, 9, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListPayments_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new BffTestFactory(_ => throw new InvalidOperationException("should not call InvoicingApi"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/bff/payments");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListPayments_ForwardsPagingToInvoicingApi()
    {
        string? receivedQuery = null;
        using var factory = new BffTestFactory(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/payments")
            {
                receivedQuery = request.RequestUri.Query;
                return JsonResponse(HttpStatusCode.OK, """{"items":[],"page":3,"pageSize":20,"totalRecords":0,"totalPages":0}""");
            }

            return JsonResponse(HttpStatusCode.OK, ValidLoginJson);
        });
        using var client = await AuthenticatedAsync(factory);

        var response = await client.GetAsync("/bff/payments?page=3&pageSize=20");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        receivedQuery.ShouldBe("?page=3&pageSize=20");
    }

    [Fact]
    public async Task CreatePayment_WhenApiRejectsBody_PassesValidationProblemThrough()
    {
        const string validationProblem = """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Amount":["'Amount' must be greater than '0'."]}}""";
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath.EndsWith("/payments", StringComparison.Ordinal)
            ? ProblemResponse(HttpStatusCode.BadRequest, validationProblem)
            : JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.PostAsJsonAsync($"/bff/invoices/{InvoiceId}/payments",
            new CreatePaymentRequest(0m, PaidOn, PaymentMethod.Card, Notes: null));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldBe(validationProblem);
    }

    [Fact]
    public async Task CreatePayment_WithValidBody_ForwardsToTheInvoicesPaymentsRoute()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == $"/invoices/{InvoiceId}/payments" && request.Method == HttpMethod.Post
            ? JsonResponse(HttpStatusCode.Created, $$"""{"id":"{{PaymentId}}"}""")
            : JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.PostAsJsonAsync($"/bff/invoices/{InvoiceId}/payments",
            new CreatePaymentRequest(125.50m, PaidOn, PaymentMethod.BankTransfer, "Part payment"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePayment_WhenInvoiceIsDraftUpstream_ReturnsConflict()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == $"/invoices/{InvoiceId}/payments"
            ? JsonResponse(HttpStatusCode.Conflict, """{"title":"Cannot record a payment on a draft or voided invoice."}""")
            : JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.PostAsJsonAsync($"/bff/invoices/{InvoiceId}/payments",
            new CreatePaymentRequest(10m, PaidOn, PaymentMethod.Cash, Notes: null));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdatePayment_ForwardsToInvoicingApi()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == $"/invoices/{InvoiceId}/payments/{PaymentId}" && request.Method == HttpMethod.Put
            ? JsonResponse(HttpStatusCode.OK, $$"""{"id":"{{PaymentId}}"}""")
            : JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.PutAsJsonAsync($"/bff/invoices/{InvoiceId}/payments/{PaymentId}",
            new UpdatePaymentRequest(80m, PaidOn, PaymentMethod.Check, Notes: null, Version: 1));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePayment_WhenSuccessful_ReturnsNoContent()
    {
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == $"/invoices/{InvoiceId}/payments/{PaymentId}" && request.Method == HttpMethod.Delete
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.DeleteAsync($"/bff/invoices/{InvoiceId}/payments/{PaymentId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
