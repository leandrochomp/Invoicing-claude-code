using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Tests.Features.Invoices;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Infrastructure.Data;

[Collection(PostgresCollection.Name)]
public class PagingExtensionsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private InvoicingDbContext _context = null!; // Assigned in InitializeAsync, before any test runs.

    public async Task InitializeAsync() => _context = await InvoiceHandlerTestData.CreateContextAsync(postgres);

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // Seeds `count` clients whose email shares a unique marker, and returns a query scoped to them
    // so other tests' rows in the shared database don't affect the counts.
    private async Task<IQueryable<string>> SeedEmailsAsync(int count)
    {
        var marker = Guid.NewGuid().ToString("N");
        for (var i = 0; i < count; i++)
        {
            var client = await InvoiceHandlerTestData.SeedClientAsync(_context);
            client.Email = $"{marker}-{i:D2}@acme.test";
        }

        await _context.SaveChangesAsync();

        return _context.Clients.AsNoTracking()
            .Where(c => c.Email.StartsWith(marker))
            .OrderBy(c => c.Email)
            .Select(c => c.Email);
    }

    [Fact]
    public async Task Returns_requested_page_with_totals()
    {
        var query = await SeedEmailsAsync(5);

        var result = await query.ToPagedAsync(page: 2, pageSize: 2);

        result.Items.Count.ShouldBe(2);
        result.Items[0].ShouldEndWith("-02@acme.test");
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(2);
        result.TotalRecords.ShouldBe(5);
        result.TotalPages.ShouldBe(3);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-3, 201)]
    public async Task Clamps_out_of_range_page_and_page_size(int page, int pageSize)
    {
        var query = await SeedEmailsAsync(3);

        var result = await query.ToPagedAsync(page, pageSize);

        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(PagingExtensions.DefaultPageSize);
        result.Items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Empty_query_has_zero_pages()
    {
        var query = await SeedEmailsAsync(0);

        var result = await query.ToPagedAsync(page: 1, pageSize: 10);

        result.Items.ShouldBeEmpty();
        result.TotalRecords.ShouldBe(0);
        result.TotalPages.ShouldBe(0);
    }
}
