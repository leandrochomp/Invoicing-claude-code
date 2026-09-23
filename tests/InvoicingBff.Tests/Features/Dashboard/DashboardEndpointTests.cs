using System.Net;
using InvoicingBff.Tests.TestSupport;
using Shouldly;
using static InvoicingBff.Tests.TestSupport.BffTestClient;

namespace InvoicingBff.Tests.Features.Dashboard;

public class DashboardEndpointTests
{
    [Fact]
    public async Task GetDashboard_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new BffTestFactory(_ => throw new InvalidOperationException("should not call InvoicingApi"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/bff/dashboard");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_PassesTheSummaryThroughUnchanged()
    {
        const string summaryJson = """{"totals":[],"counts":{"draft":1,"outstanding":0,"overdue":0,"paid":0},"dueInvoices":[],"recentPayments":[]}""";
        using var factory = new BffTestFactory(request => request.RequestUri!.AbsolutePath == "/dashboard"
            ? JsonResponse(HttpStatusCode.OK, summaryJson)
            : JsonResponse(HttpStatusCode.OK, ValidLoginJson));
        using var client = await AuthenticatedAsync(factory);

        var response = await client.GetAsync("/bff/dashboard");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe(summaryJson);
    }
}
