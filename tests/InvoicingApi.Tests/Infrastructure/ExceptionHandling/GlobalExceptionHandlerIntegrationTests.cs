using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace InvoicingApi.Tests.Infrastructure.ExceptionHandling;

[Collection(PostgresCollection.Name)]
public class GlobalExceptionHandlerIntegrationTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Unhandled_exception_returns_problem_details_response()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:Default", postgres.ConnectionString);
                TestJwt.Apply(builder);
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/__test/throw");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
