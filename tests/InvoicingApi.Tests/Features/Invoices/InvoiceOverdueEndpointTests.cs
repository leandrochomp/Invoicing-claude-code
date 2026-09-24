using System.Net.Http.Json;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

// Overdue is derived when read, so GET by id and the status filter must agree on the same rule.
[Collection(PostgresCollection.Name)]
public class InvoiceOverdueEndpointTests(PostgresFixture postgres)
{
    // Pinned near the real time so test tokens stay valid; only the calendar day matters here.
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateTimeOffset Today = new(Now.UtcDateTime.Date, TimeSpan.Zero);

    private async Task<WebApplicationFactory<Program>> CreateFactoryAsync()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", postgres.ConnectionString);
                TestJwt.Apply(builder);
                TestClock.Apply(builder, Now);
            });

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        await context.Database.MigrateAsync();

        return factory;
    }

    private static async Task<Invoice> SeedInvoiceAsync(WebApplicationFactory<Program> factory, InvoiceStatus status, DateTimeOffset dueDate)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        return await InvoiceHandlerTestData.SeedInvoiceAsync(context, status, dueDate: dueDate);
    }

    [Fact]
    public async Task Sent_invoice_past_its_due_date_reads_as_overdue()
    {
        await using var factory = await CreateFactoryAsync();
        var overdue = await SeedInvoiceAsync(factory, InvoiceStatus.Sent, Today.AddDays(-1));
        var dueToday = await SeedInvoiceAsync(factory, InvoiceStatus.Sent, Today);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var overdueBody = await httpClient.GetFromJsonAsync<InvoiceDto>($"/invoices/{overdue.Id}");
        var dueTodayBody = await httpClient.GetFromJsonAsync<InvoiceDto>($"/invoices/{dueToday.Id}");

        overdueBody.ShouldNotBeNull();
        overdueBody.Status.ShouldBe(InvoiceStatus.Overdue);
        dueTodayBody.ShouldNotBeNull();
        dueTodayBody.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task Overdue_filter_returns_sent_invoices_past_due_and_sent_filter_excludes_them()
    {
        await using var factory = await CreateFactoryAsync();
        var overdue = await SeedInvoiceAsync(factory, InvoiceStatus.Sent, Today.AddDays(-1));
        var current = await SeedInvoiceAsync(factory, InvoiceStatus.Sent, Today.AddDays(10));
        var paidLate = await SeedInvoiceAsync(factory, InvoiceStatus.Paid, Today.AddDays(-1));
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var overdueList = await ListAsync(httpClient, overdue.ClientId, InvoiceStatus.Overdue);
        var sentList = await ListAsync(httpClient, current.ClientId, InvoiceStatus.Sent);
        var sentListForOverdueClient = await ListAsync(httpClient, overdue.ClientId, InvoiceStatus.Sent);
        var overdueListForPaidClient = await ListAsync(httpClient, paidLate.ClientId, InvoiceStatus.Overdue);

        overdueList.Items.ShouldHaveSingleItem().Status.ShouldBe(InvoiceStatus.Overdue);
        sentList.Items.ShouldHaveSingleItem().Status.ShouldBe(InvoiceStatus.Sent);
        sentListForOverdueClient.Items.ShouldBeEmpty();
        overdueListForPaidClient.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unfiltered_list_reports_derived_status()
    {
        await using var factory = await CreateFactoryAsync();
        var overdue = await SeedInvoiceAsync(factory, InvoiceStatus.Sent, Today.AddDays(-3));
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var list = await httpClient.GetFromJsonAsync<PagedResponse<InvoiceSummaryDto>>($"/invoices?clientId={overdue.ClientId}");

        list.ShouldNotBeNull();
        list.Items.ShouldHaveSingleItem().Status.ShouldBe(InvoiceStatus.Overdue);
    }

    private static async Task<PagedResponse<InvoiceSummaryDto>> ListAsync(HttpClient httpClient, Guid clientId, InvoiceStatus status)
    {
        var list = await httpClient.GetFromJsonAsync<PagedResponse<InvoiceSummaryDto>>($"/invoices?clientId={clientId}&status={status}");
        list.ShouldNotBeNull();
        return list;
    }
}
