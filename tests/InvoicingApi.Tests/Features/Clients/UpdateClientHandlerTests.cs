using Ardalis.Result;
using InvoicingApi.Features.Clients;
using InvoicingApi.Tests.Features.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

[Collection(PostgresCollection.Name)]
public class UpdateClientHandlerTests(PostgresFixture postgres)
{
    private static UpdateClientRequest UpdateRequest() => new(
        CompanyName: "New Name",
        ContactName: "Jane Doe",
        Email: $"{Guid.NewGuid()}@acme.test",
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
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var logger = Substitute.For<ILogger<UpdateClientHandler>>();
        var handler = new UpdateClientHandler(context, logger);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id, UpdateRequest());

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, id.ToString());
    }

    [Fact]
    public async Task Updates_client_and_saves()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var client = await InvoiceHandlerTestData.SeedClientAsync(context);
        context.ChangeTracker.Clear();
        var logger = Substitute.For<ILogger<UpdateClientHandler>>();
        var handler = new UpdateClientHandler(context, logger);
        var request = UpdateRequest();

        var result = await handler.HandleAsync(client.Id, request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CompanyName.ShouldBe("New Name");
        var saved = await context.Clients.AsNoTracking().SingleAsync(c => c.Id == client.Id);
        saved.Email.ShouldBe(request.Email);
        saved.IsActive.ShouldBeFalse();
        saved.PreferredCurrency.ShouldBe("EUR");
        logger.ReceivedLog(LogLevel.Information, client.Id.ToString());
    }
}
