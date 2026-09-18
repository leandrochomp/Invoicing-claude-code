using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoicingApi.Tests;

[Collection(PostgresCollection.Name)]
public class HealthEndpointTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Health_endpoint_responds()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", postgres.ConnectionString);
                TestJwt.Apply(builder);
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected the health endpoint to report Healthy, got {response.StatusCode}: {body}");
    }
}
