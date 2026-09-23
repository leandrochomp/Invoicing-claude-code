using Ardalis.Result;
using InvoicingApi.Features.Clients;
using InvoicingApi.Tests.Features.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

[Collection(PostgresCollection.Name)]
public class CreateClientHandlerTests(PostgresFixture postgres)
{
    private static CreateClientRequest ValidRequest() => new(
        CompanyName: "Acme Corp",
        ContactName: "Jane Doe",
        Email: $"{Guid.NewGuid()}@acme.test",
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
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var logger = Substitute.For<ILogger<CreateClientHandler>>();
        var handler = new CreateClientHandler(context, logger);
        var request = ValidRequest();

        var result = await handler.HandleAsync(request);

        result.Status.ShouldBe(ResultStatus.Created);
        result.Value.CompanyName.ShouldBe("Acme Corp");
        result.Value.Email.ShouldBe(request.Email);
        (await context.Clients.AsNoTracking().AnyAsync(c => c.Id == result.Value.Id)).ShouldBeTrue();
        logger.ReceivedLog(LogLevel.Information, result.Value.Id.ToString());
    }
}
