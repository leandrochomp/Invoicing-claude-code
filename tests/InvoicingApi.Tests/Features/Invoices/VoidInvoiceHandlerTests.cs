using Ardalis.Result;
using InvoicingApi.Features.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class VoidInvoiceHandlerTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Voids_sent_invoice_without_payments()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Sent);
        var logger = Substitute.For<ILogger<VoidInvoiceHandler>>();
        var handler = new VoidInvoiceHandler(context, TimeProvider.System, logger);

        var result = await handler.HandleAsync(invoice.Id, new VoidInvoiceRequest(invoice.Version));

        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.Status.ShouldBe(InvoiceStatus.Void);
        result.Value.Version.ShouldBe(invoice.Version + 1);
        (await context.Invoices.AsNoTracking().SingleAsync(i => i.Id == invoice.Id)).Status.ShouldBe(InvoiceStatus.Void);
        logger.ReceivedLog(LogLevel.Information, invoice.Id.ToString());
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Void)]
    public async Task Returns_conflict_when_not_sent(InvoiceStatus status)
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, status);
        var logger = Substitute.For<ILogger<VoidInvoiceHandler>>();
        var handler = new VoidInvoiceHandler(context, TimeProvider.System, logger);

        var result = await handler.HandleAsync(invoice.Id, new VoidInvoiceRequest(invoice.Version));

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await context.Invoices.AsNoTracking().SingleAsync(i => i.Id == invoice.Id)).Status.ShouldBe(status);
        logger.ReceivedLog(LogLevel.Warning, invoice.Id.ToString());
    }

    [Fact]
    public async Task Returns_conflict_when_sent_invoice_has_payments()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Sent, paymentAmount: 40m);
        var logger = Substitute.For<ILogger<VoidInvoiceHandler>>();
        var handler = new VoidInvoiceHandler(context, TimeProvider.System, logger);

        var result = await handler.HandleAsync(invoice.Id, new VoidInvoiceRequest(invoice.Version));

        result.Status.ShouldBe(ResultStatus.Conflict);
        (await context.Invoices.AsNoTracking().SingleAsync(i => i.Id == invoice.Id)).Status.ShouldBe(InvoiceStatus.Sent);
        logger.ReceivedLog(LogLevel.Warning, "payments");
    }

    [Fact]
    public async Task Returns_conflict_when_version_is_stale()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Sent);
        var logger = Substitute.For<ILogger<VoidInvoiceHandler>>();
        var handler = new VoidInvoiceHandler(context, TimeProvider.System, logger);

        var result = await handler.HandleAsync(invoice.Id, new VoidInvoiceRequest(invoice.Version + 1));

        result.Status.ShouldBe(ResultStatus.Conflict);
        logger.ReceivedLog(LogLevel.Warning, "Concurrency conflict");
    }

    [Fact]
    public async Task Returns_not_found_when_invoice_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var logger = Substitute.For<ILogger<VoidInvoiceHandler>>();
        var handler = new VoidInvoiceHandler(context, TimeProvider.System, logger);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id, new VoidInvoiceRequest(0));

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, id.ToString());
    }
}
