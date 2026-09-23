using Ardalis.Result;
using InvoicingApi.Features.Invoices;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

public class DeleteInvoiceHandlerTests
{
    private static Invoice CreateInvoice() => new()
    {
        ClientId = Guid.NewGuid(),
        IssueDate = DateTimeOffset.UtcNow,
        DueDate = DateTimeOffset.UtcNow.AddDays(30),
        Currency = "USD",
    };

    [Fact]
    public async Task Returns_not_found_when_invoice_does_not_exist()
    {
        var repository = Substitute.For<IRepository<Invoice>>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<DeleteInvoiceHandler>>();
        var handler = new DeleteInvoiceHandler(repository, unitOfWork, logger);

        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id);

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, id.ToString());
    }

    [Fact]
    public async Task Removes_invoice_and_saves_when_it_exists()
    {
        var invoice = CreateInvoice();
        var repository = Substitute.For<IRepository<Invoice>>();
        repository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<DeleteInvoiceHandler>>();
        var handler = new DeleteInvoiceHandler(repository, unitOfWork, logger);

        var result = await handler.HandleAsync(invoice.Id);

        result.IsSuccess.ShouldBeTrue();
        repository.Received(1).Remove(invoice);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        logger.ReceivedLog(LogLevel.Information, invoice.Id.ToString());
    }
}
