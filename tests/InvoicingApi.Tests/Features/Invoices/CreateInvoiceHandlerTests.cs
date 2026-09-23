using Ardalis.Result;
using InvoicingApi.Features.Invoices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class CreateInvoiceHandlerTests(PostgresFixture postgres)
{
    private static CreateInvoiceRequest ValidRequest(Guid clientId) => new(
        ClientId: clientId,
        IssueDate: DateTimeOffset.UtcNow,
        DueDate: DateTimeOffset.UtcNow.AddDays(30),
        Currency: "USD",
        Notes: null,
        Items: [new CreateInvoiceItemRequest("Consulting", 1, 100m, 0m, 0)]);

    [Fact]
    public async Task Logs_created_invoice()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var client = await InvoiceHandlerTestData.SeedClientAsync(context);
        var logger = Substitute.For<ILogger<CreateInvoiceHandler>>();
        var handler = new CreateInvoiceHandler(context, logger);

        var result = await handler.HandleAsync(ValidRequest(client.Id));

        result.Status.ShouldBe(ResultStatus.Created);
        logger.ReceivedLog(LogLevel.Information, result.Value.Id.ToString());
    }

    [Fact]
    public async Task Logs_warning_when_client_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var logger = Substitute.For<ILogger<CreateInvoiceHandler>>();
        var handler = new CreateInvoiceHandler(context, logger);
        var clientId = Guid.NewGuid();

        var result = await handler.HandleAsync(ValidRequest(clientId));

        result.Status.ShouldBe(ResultStatus.Invalid);
        logger.ReceivedLog(LogLevel.Warning, clientId.ToString());
    }
}
