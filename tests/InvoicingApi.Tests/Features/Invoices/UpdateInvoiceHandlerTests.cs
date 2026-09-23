using Ardalis.Result;
using InvoicingApi.Features.Invoices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class UpdateInvoiceHandlerTests(PostgresFixture postgres)
{
    private static UpdateInvoiceRequest RequestFor(Invoice invoice, int version) => new(
        ClientId: invoice.ClientId,
        Status: InvoiceStatus.Sent,
        IssueDate: invoice.IssueDate,
        DueDate: invoice.DueDate,
        Currency: "USD",
        Notes: "Updated",
        Version: version,
        Items: [new UpdateInvoiceItemRequest(null, "Consulting", 2, 100m, 0m, 0)]);

    [Fact]
    public async Task Logs_updated_invoice()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context);
        var logger = Substitute.For<ILogger<UpdateInvoiceHandler>>();
        var handler = new UpdateInvoiceHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id, RequestFor(invoice, invoice.Version));

        result.Status.ShouldBe(ResultStatus.Ok);
        logger.ReceivedLog(LogLevel.Information, invoice.Id.ToString());
    }

    [Fact]
    public async Task Logs_warning_when_invoice_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context);
        var logger = Substitute.For<ILogger<UpdateInvoiceHandler>>();
        var handler = new UpdateInvoiceHandler(context, logger);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id, RequestFor(invoice, 0));

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, id.ToString());
    }

    [Fact]
    public async Task Logs_warning_on_concurrency_conflict()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context);
        var logger = Substitute.For<ILogger<UpdateInvoiceHandler>>();
        var handler = new UpdateInvoiceHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id, RequestFor(invoice, invoice.Version + 1));

        result.Status.ShouldBe(ResultStatus.Conflict);
        logger.ReceivedLog(LogLevel.Warning, "Concurrency conflict");
    }
}
