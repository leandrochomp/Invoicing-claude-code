using Ardalis.Result;
using InvoicingApi.Features.Invoices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class UpdatePaymentHandlerTests(PostgresFixture postgres)
{
    private static UpdatePaymentRequest Request(decimal amount, int version) =>
        new(amount, DateTimeOffset.UtcNow, PaymentMethod.Card, null, version);

    [Fact]
    public async Task Logs_updated_payment()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, paymentAmount: 40m);
        var payment = invoice.Payments.Single();
        var logger = Substitute.For<ILogger<UpdatePaymentHandler>>();
        var handler = new UpdatePaymentHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id, payment.Id, Request(60m, payment.Version));

        result.Status.ShouldBe(ResultStatus.Ok);
        logger.ReceivedLog(LogLevel.Information, payment.Id.ToString());
    }

    [Fact]
    public async Task Logs_warning_when_payment_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context);
        var logger = Substitute.For<ILogger<UpdatePaymentHandler>>();
        var handler = new UpdatePaymentHandler(context, logger);
        var paymentId = Guid.NewGuid();

        var result = await handler.HandleAsync(invoice.Id, paymentId, Request(40m, 0));

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, paymentId.ToString());
    }

    [Fact]
    public async Task Logs_warning_on_concurrency_conflict()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var invoice = await InvoiceHandlerTestData.SeedInvoiceAsync(context, paymentAmount: 40m);
        var payment = invoice.Payments.Single();
        var logger = Substitute.For<ILogger<UpdatePaymentHandler>>();
        var handler = new UpdatePaymentHandler(context, logger);

        var result = await handler.HandleAsync(invoice.Id, payment.Id, Request(60m, payment.Version + 1));

        result.Status.ShouldBe(ResultStatus.Conflict);
        logger.ReceivedLog(LogLevel.Warning, "Concurrency conflict");
    }
}
