using InvoicingApi.Features.Clients;
using NSubstitute;
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

public class ListClientsQueryTests
{
    [Fact]
    public async Task Returns_summaries_for_all_clients()
    {
        var clients = new List<Client>
        {
            new()
            {
                CompanyName = "Acme Corp",
                Email = "billing@acme.test",
                AddressLine1 = "1 Main St",
                City = "Springfield",
                StateOrRegion = "IL",
                PostalCode = "62701",
                Country = "US",
                PreferredCurrency = "USD",
            },
            new()
            {
                CompanyName = "Globex Inc",
                Email = "ap@globex.test",
                AddressLine1 = "2 Main St",
                City = "Springfield",
                StateOrRegion = "IL",
                PostalCode = "62701",
                Country = "US",
                PreferredCurrency = "USD",
            },
        };
        var repository = Substitute.For<IRepository<Client>>();
        repository.ListAsync(Arg.Any<CancellationToken>()).Returns(clients);
        var query = new ListClientsQuery(repository);

        var result = await query.ListAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(c => c.CompanyName == "Acme Corp");
        result.Value.ShouldContain(c => c.CompanyName == "Globex Inc");
    }
}
