using Ardalis.Result;
using InvoicingApi.Features.Clients;
using InvoicingApi.Tests.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

[Collection(PostgresCollection.Name)]
public class ClientQueriesTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Returns_not_found_when_client_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var queries = new ClientQueries(context);

        var result = await queries.GetByIdAsync(Guid.NewGuid());

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Returns_detail_when_client_exists()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var client = await InvoiceHandlerTestData.SeedClientAsync(context);
        var queries = new ClientQueries(context);

        var result = await queries.GetByIdAsync(client.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(client.Id);
        result.Value.CompanyName.ShouldBe("Acme Corp");
        result.Value.Email.ShouldBe(client.Email);
        result.Value.AddressLine1.ShouldBe("1 Main St");
        result.Value.City.ShouldBe("Springfield");
        result.Value.PreferredCurrency.ShouldBe("USD");
        result.Value.IsActive.ShouldBeTrue();
    }
}
