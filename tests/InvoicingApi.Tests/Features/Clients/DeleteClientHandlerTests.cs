using Ardalis.Result;
using InvoicingApi.Features.Clients;
using InvoicingApi.Tests.Features.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

[Collection(PostgresCollection.Name)]
public class DeleteClientHandlerTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    private static TimeProvider FixedClock()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(FixedNow);
        return clock;
    }

    [Fact]
    public async Task Returns_not_found_when_client_does_not_exist()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var logger = Substitute.For<ILogger<DeleteClientHandler>>();
        var handler = new DeleteClientHandler(context, FixedClock(), logger);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(id, Guid.NewGuid());

        result.Status.ShouldBe(ResultStatus.NotFound);
        logger.ReceivedLog(LogLevel.Warning, id.ToString());
    }

    [Fact]
    public async Task Soft_deletes_by_setting_deleted_audit_fields_without_removing()
    {
        await using var context = await InvoiceHandlerTestData.CreateContextAsync(postgres);
        var client = await InvoiceHandlerTestData.SeedClientAsync(context);
        context.ChangeTracker.Clear();
        var logger = Substitute.For<ILogger<DeleteClientHandler>>();
        var handler = new DeleteClientHandler(context, FixedClock(), logger);
        var deletedBy = Guid.NewGuid();

        var result = await handler.HandleAsync(client.Id, deletedBy);

        result.Status.ShouldBe(ResultStatus.NoContent);
        var saved = await context.Clients.IgnoreQueryFilters().AsNoTracking().SingleAsync(c => c.Id == client.Id);
        saved.IsDeleted.ShouldBeTrue();
        saved.DeletedAt.ShouldBe(FixedNow);
        saved.DeletedBy.ShouldBe(deletedBy);
        logger.ReceivedLog(LogLevel.Information, client.Id.ToString());
    }
}
