using Ardalis.Result;
using InvoicingApi.Features.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class UpdateInvoiceHandlerTests(PostgresFixture postgres)
{
    private static UpdateInvoiceRequest RequestFor(Invoice invoice, int version) => new(
        ClientId: invoice.ClientId,
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
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Draft);
        var logger = Substitute.For<ILogger<UpdateInvoiceHandler>>();
        var handler = new UpdateInvoiceHandler(context, TimeProvider.System, logger);

        var result = await handler.HandleAsync(invoice.Id, RequestFor(invoice, invoice.Version));

        result.Status.ShouldBe(ResultStatus.Ok);
        logger.ReceivedLog(LogLevel.Information, invoice.Id.ToString());
    }

    [Fact]
    public async Task Logs_warning_when_invoice_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Draft);
        var logger = Substitute.For<ILogger<UpdateInvoiceHandler>>();
        var handler = new UpdateInvoiceHandler(context, TimeProvider.System, logger);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id, RequestFor(invoice, 0));

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, id.ToString());
    }

    [Fact]
    public async Task Logs_warning_on_concurrency_conflict()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Draft);
        var logger = Substitute.For<ILogger<UpdateInvoiceHandler>>();
        var handler = new UpdateInvoiceHandler(context, TimeProvider.System, logger);

        var result = await handler.HandleAsync(invoice.Id, RequestFor(invoice, invoice.Version + 1));

        result.Status.ShouldBe(ResultStatus.Conflict);
        logger.ReceivedLog(LogLevel.Warning, "Concurrency conflict");
    }

    // The stored invoice's content exactly (as a client would echo it back, at the database's
    // microsecond precision), with only the notes changed.
    private async Task<UpdateInvoiceRequest> NotesOnlyRequestAsync(Guid id)
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var stored = await context.Invoices.AsNoTracking().Include(i => i.Items).SingleAsync(i => i.Id == id);
        return new UpdateInvoiceRequest(
            stored.ClientId,
            stored.IssueDate,
            stored.DueDate,
            stored.Currency,
            "Paid by bank transfer, please",
            stored.Version,
            stored.Items.Select(i => new UpdateInvoiceItemRequest(i.Id, i.Description, i.Quantity, i.UnitPrice, i.TaxRate, i.SortOrder)).ToList());
    }

    [Fact]
    public async Task Sent_invoice_accepts_a_notes_only_change()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Sent);
        var handler = new UpdateInvoiceHandler(context, TimeProvider.System, Substitute.For<ILogger<UpdateInvoiceHandler>>());

        var result = await handler.HandleAsync(invoice.Id, await NotesOnlyRequestAsync(invoice.Id));

        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.Notes.ShouldBe("Paid by bank transfer, please");
        result.Value.Status.ShouldBe(InvoiceStatus.Sent);
        result.Value.GrandTotal.ShouldBe(100m);
    }

    [Fact]
    public async Task Sent_invoice_rejects_a_content_change()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Sent);
        var logger = Substitute.For<ILogger<UpdateInvoiceHandler>>();
        var handler = new UpdateInvoiceHandler(context, TimeProvider.System, logger);

        var result = await handler.HandleAsync(invoice.Id, RequestFor(invoice, invoice.Version));

        result.Status.ShouldBe(ResultStatus.Conflict);
        logger.ReceivedLog(LogLevel.Warning, invoice.Id.ToString());
        (await context.Invoices.AsNoTracking().SingleAsync(i => i.Id == invoice.Id)).GrandTotal.ShouldBe(100m);
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Void)]
    public async Task Final_invoice_rejects_even_a_notes_only_change(InvoiceStatus status)
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, status);
        var logger = Substitute.For<ILogger<UpdateInvoiceHandler>>();
        var handler = new UpdateInvoiceHandler(context, TimeProvider.System, logger);

        var result = await handler.HandleAsync(invoice.Id, await NotesOnlyRequestAsync(invoice.Id));

        result.Status.ShouldBe(ResultStatus.Conflict);
        logger.ReceivedLog(LogLevel.Warning, invoice.Id.ToString());
    }
}
