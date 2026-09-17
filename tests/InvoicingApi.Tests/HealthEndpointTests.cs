using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoicingApi.Tests;

public class HealthEndpointTests
{
    [Fact]
    public async Task Health_endpoint_responds()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting(
                    "ConnectionStrings:Default",
                    "Host=localhost;Database=invoicing;Username=postgres;Password=postgres");
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.ServiceUnavailable,
            $"Expected the health endpoint to respond, got {response.StatusCode}: {body}");
    }
}
