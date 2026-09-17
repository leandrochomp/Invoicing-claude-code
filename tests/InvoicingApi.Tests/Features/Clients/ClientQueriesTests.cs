using Ardalis.Result;
using InvoicingApi.Features.Clients;
using NSubstitute;
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

public class ClientQueriesTests
{
    private static Client CreateClient() => new()
    {
        CompanyName = "Acme Corp",
        Email = "billing@acme.test",
        AddressLine1 = "1 Main St",
        City = "Springfield",
        StateOrRegion = "IL",
        PostalCode = "62701",
        Country = "US",
        PreferredCurrency = "USD",
    };

    [Fact]
    public async Task Returns_not_found_when_client_does_not_exist()
    {
        var repository = Substitute.For<IRepository<Client>>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Client?)null);
        var queries = new ClientQueries(repository);

        var result = await queries.GetByIdAsync(Guid.NewGuid());

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Returns_summary_when_client_exists()
    {
        var client = CreateClient();
        var repository = Substitute.For<IRepository<Client>>();
        repository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        var queries = new ClientQueries(repository);

        var result = await queries.GetByIdAsync(client.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(client.Id);
        result.Value.CompanyName.ShouldBe("Acme Corp");
        result.Value.Email.ShouldBe("billing@acme.test");
    }
}
