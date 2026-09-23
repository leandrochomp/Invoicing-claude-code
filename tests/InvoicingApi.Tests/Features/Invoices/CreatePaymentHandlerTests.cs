using Ardalis.Result;
using InvoicingApi.Features.Invoices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class CreatePaymentHandlerTests(PostgresFixture postgres)
{
    private static CreatePaymentRequest Request(decimal amount) =>
        new(amount, DateTimeOffset.UtcNow, PaymentMethod.BankTransfer, null);

    [Fact]
    public async Task Logs_recorded_payment()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context);
        var logger = Substitute.For<ILogger<CreatePaymentHandler>>();
        var handler = new CreatePaymentHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id, Request(40m));

        result.Status.ShouldBe(ResultStatus.Created);
        logger.ReceivedLog(LogLevel.Information, result.Value.Id.ToString());
    }

    [Fact]
    public async Task Logs_warning_when_invoice_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var logger = Substitute.For<ILogger<CreatePaymentHandler>>();
        var handler = new CreatePaymentHandler(context, logger);
        var invoiceId = Guid.NewGuid();

        var result = await handler.HandleAsync(invoiceId, Request(40m));

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, invoiceId.ToString());
    }

    [Fact]
    public async Task Logs_warning_when_invoice_is_draft()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, InvoiceStatus.Draft);
        var logger = Substitute.For<ILogger<CreatePaymentHandler>>();
        var handler = new CreatePaymentHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id, Request(40m));

        result.Status.ShouldBe(ResultStatus.Conflict);
        logger.ReceivedLog(LogLevel.Warning, nameof(InvoiceStatus.Draft));
    }

    [Fact]
    public async Task Logs_warning_when_amount_exceeds_balance()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context);
        var logger = Substitute.For<ILogger<CreatePaymentHandler>>();
        var handler = new CreatePaymentHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id, Request(150m));

        result.Status.ShouldBe(ResultStatus.Invalid);
        logger.ReceivedLog(LogLevel.Warning, "exceeds remaining balance");
    }
}
