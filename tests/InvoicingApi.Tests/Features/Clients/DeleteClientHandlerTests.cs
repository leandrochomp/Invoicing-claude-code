using Ardalis.Result;
using InvoicingApi.Features.Clients;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

public class DeleteClientHandlerTests
{
    private static Client ExistingClient() => new()
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

    private static readonly DateTimeOffset FixedNow = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    private static TimeProvider FixedClock()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(FixedNow);
        return clock;
    }

    [Fact]
    public async Task Returns_not_found_when_client_does_not_exist()
    {
        var repository = Substitute.For<IRepository<Client>>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Client?)null);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<DeleteClientHandler>>();
        var handler = new DeleteClientHandler(repository, unitOfWork, FixedClock(), logger);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id, Guid.NewGuid());

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, id.ToString());
    }

    [Fact]
    public async Task Soft_deletes_by_setting_deleted_audit_fields_without_removing()
    {
        var client = ExistingClient();
        var repository = Substitute.For<IRepository<Client>>();
        repository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<DeleteClientHandler>>();
        var handler = new DeleteClientHandler(repository, unitOfWork, FixedClock(), logger);
        var deletedBy = Guid.NewGuid();

        var result = await handler.HandleAsync(client.Id, deletedBy);

        result.Status.ShouldBe(ResultStatus.NoContent);
        client.IsDeleted.ShouldBeTrue();
        client.DeletedAt.ShouldBe(FixedNow);
        client.DeletedBy.ShouldBe(deletedBy);
        repository.DidNotReceive().Remove(Arg.Any<Client>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        logger.ReceivedLog(LogLevel.Information, client.Id.ToString());
    }
}
