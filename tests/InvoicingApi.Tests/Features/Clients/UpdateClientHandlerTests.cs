using Ardalis.Result;
using InvoicingApi.Features.Clients;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

public class UpdateClientHandlerTests
{
    private static Client ExistingClient() => new()
    {
        CompanyName = "Old Name",
        Email = "old@acme.test",
        AddressLine1 = "1 Main St",
        City = "Springfield",
        StateOrRegion = "IL",
        PostalCode = "62701",
        Country = "US",
        PreferredCurrency = "USD",
    };

    private static UpdateClientRequest UpdateRequest() => new(
        CompanyName: "New Name",
        ContactName: "Jane Doe",
        Email: "new@acme.test",
        Phone: "555-0100",
        AddressLine1: "2 Main St",
        AddressLine2: null,
        City: "Springfield",
        StateOrRegion: "IL",
        PostalCode: "62701",
        Country: "US",
        PreferredCurrency: "EUR",
        IsActive: false);

    [Fact]
    public async Task Returns_not_found_when_client_does_not_exist()
    {
        var repository = Substitute.For<IRepository<Client>>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Client?)null);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<UpdateClientHandler>>();
        var handler = new UpdateClientHandler(repository, unitOfWork, logger);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id, UpdateRequest());

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, id.ToString());
    }

    [Fact]
    public async Task Updates_tracked_entity_and_saves()
    {
        var client = ExistingClient();
        var repository = Substitute.For<IRepository<Client>>();
        repository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<UpdateClientHandler>>();
        var handler = new UpdateClientHandler(repository, unitOfWork, logger);

        var result = await handler.HandleAsync(client.Id, UpdateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value.CompanyName.ShouldBe("New Name");
        client.Email.ShouldBe("new@acme.test");
        client.IsActive.ShouldBeFalse();
        client.PreferredCurrency.ShouldBe("EUR");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        logger.ReceivedLog(LogLevel.Information, client.Id.ToString());
    }
}
