using Ardalis.Result;
using InvoicingApi.Features.Clients;
using NSubstitute;
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

public class CreateClientCommandTests
{
    private static CreateClientRequest ValidRequest() => new(
        CompanyName: "Acme Corp",
        ContactName: "Jane Doe",
        Email: "billing@acme.test",
        Phone: "555-0100",
        AddressLine1: "1 Main St",
        AddressLine2: null,
        City: "Springfield",
        StateOrRegion: "IL",
        PostalCode: "62701",
        Country: "US",
        PreferredCurrency: "USD");

    [Fact]
    public async Task Creates_client_and_returns_created_result()
    {
        var repository = Substitute.For<IRepository<Client>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var command = new CreateClientCommand(repository, unitOfWork);

        var result = await command.CreateAsync(ValidRequest());

        result.Status.ShouldBe(ResultStatus.Created);
        result.Value.CompanyName.ShouldBe("Acme Corp");
        result.Value.Email.ShouldBe("billing@acme.test");
        await repository.Received(1).AddAsync(Arg.Is<Client>(c => c.CompanyName == "Acme Corp"), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
