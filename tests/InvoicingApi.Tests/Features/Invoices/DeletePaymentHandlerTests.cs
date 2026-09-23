using Ardalis.Result;
using InvoicingApi.Features.Invoices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class DeletePaymentHandlerTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Logs_deleted_payment()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, paymentAmount: 40m);
        var payment = invoice.Payments.Single();
        var logger = Substitute.For<ILogger<DeletePaymentHandler>>();
        var handler = new DeletePaymentHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id, payment.Id);

        result.Status.ShouldBe(ResultStatus.NoContent);
        logger.ReceivedLog(LogLevel.Information, payment.Id.ToString());
    }

    [Fact]
    public async Task Logs_warning_when_invoice_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var logger = Substitute.For<ILogger<DeletePaymentHandler>>();
        var handler = new DeletePaymentHandler(context, logger);
        var invoiceId = Guid.NewGuid();

        var result = await handler.HandleAsync(invoiceId, Guid.NewGuid());

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, invoiceId.ToString());
    }
}
