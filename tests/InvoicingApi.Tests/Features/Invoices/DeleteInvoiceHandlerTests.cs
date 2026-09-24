using Ardalis.Result;
using InvoicingApi.Features.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class DeleteInvoiceHandlerTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Returns_not_found_when_invoice_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var logger = Substitute.For<ILogger<DeleteInvoiceHandler>>();
        var handler = new DeleteInvoiceHandler(context, logger);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id);

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, id.ToString());
    }

    [Fact]
    public async Task Removes_draft_invoice()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Draft);
        var logger = Substitute.For<ILogger<DeleteInvoiceHandler>>();
        var handler = new DeleteInvoiceHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id);

        result.IsSuccess.ShouldBeTrue();
        (await context.Invoices.AsNoTracking().AnyAsync(i => i.Id == invoice.Id)).ShouldBeFalse();
        logger.ReceivedLog(LogLevel.Information, invoice.Id.ToString());
    }

    [Fact]
    public async Task Returns_conflict_when_draft_has_payments()
    {
        // Drafts can't take payments any more, but rows from before that rule could still exist.
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Draft, paymentAmount: 40m);
        var logger = Substitute.For<ILogger<DeleteInvoiceHandler>>();
        var handler = new DeleteInvoiceHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id);

        result.Status.ShouldBe(ResultStatus.Conflict);
        logger.ReceivedLog(LogLevel.Warning, invoice.Id.ToString());
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Void)]
    public async Task Returns_conflict_and_keeps_invoice_when_not_draft(InvoiceStatus status)
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, status);
        var logger = Substitute.For<ILogger<DeleteInvoiceHandler>>();
        var handler = new DeleteInvoiceHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id);

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await context.Invoices.AsNoTracking().AnyAsync(i => i.Id == invoice.Id)).ShouldBeTrue();
        logger.ReceivedLog(LogLevel.Warning, invoice.Id.ToString());
    }
}
