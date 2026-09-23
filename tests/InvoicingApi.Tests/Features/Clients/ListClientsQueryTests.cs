using InvoicingApi.Features.Clients;
using InvoicingApi.Tests.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

[Collection(PostgresCollection.Name)]
public class ListClientsQueryTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Returns_summaries_for_all_clients()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var first = await InvoiceHandlerTestData.SeedClientAsync(context);
        var second = await InvoiceHandlerTestData.SeedClientAsync(context);
        var query = new ListClientsQuery(context);

        var result = await query.ListAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldContain(c => c.Id == first.Id && c.Email == first.Email);
        result.Value.ShouldContain(c => c.Id == second.Id && c.CompanyName == second.CompanyName);
    }
}
