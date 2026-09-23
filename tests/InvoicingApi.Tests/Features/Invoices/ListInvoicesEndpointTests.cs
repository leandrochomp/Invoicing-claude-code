using System.Net;
using System.Net.Http.Json;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class ListInvoicesEndpointTests(PostgresFixture postgres)
{
    private async Task<WebApplicationFactory<Program>> CreateFactoryAsync()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", postgres.ConnectionString);
                TestJwt.Apply(builder);
            });

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        await context.Database.MigrateAsync();

        return factory;
    }

    private static async Task<Guid> SeedClientAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

        var client = new Client
        {
            CompanyName = "Acme Corp",
            Email = $"{Guid.NewGuid()}@acme.test",
            AddressLine1 = "1 Main St",
            City = "Springfield",
            StateOrRegion = "IL",
            PostalCode = "62701",
            Country = "US",
            PreferredCurrency = "USD",
        };
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        return client.Id;
    }

    private static async Task SeedInvoiceAsync(WebApplicationFactory<Program> factory, Guid clientId, InvoiceStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

        var invoice = new Invoice
        {
            ClientId = clientId,
            InvoiceNumber = await InvoiceNumberGenerator.NextAsync(context),
            Status = status,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = "USD",
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Filters_by_client_id()
    {
        await using var factory = await CreateFactoryAsync();
        var clientId = await SeedClientAsync(factory);
        var otherClientId = await SeedClientAsync(factory);
        await SeedInvoiceAsync(factory, clientId, InvoiceStatus.Draft);
        await SeedInvoiceAsync(factory, clientId, InvoiceStatus.Sent);
        await SeedInvoiceAsync(factory, otherClientId, InvoiceStatus.Draft);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.GetFromJsonAsync<PagedResponse<InvoiceSummaryDto>>($"/invoices?clientId={clientId}");

        response.ShouldNotBeNull();
        response.Items.ShouldAllBe(i => i.ClientId == clientId);
        response.Items.Count.ShouldBe(2);
        response.TotalRecords.ShouldBe(2);
    }

    [Fact]
    public async Task Filters_by_status()
    {
        await using var factory = await CreateFactoryAsync();
        var clientId = await SeedClientAsync(factory);
        await SeedInvoiceAsync(factory, clientId, InvoiceStatus.Draft);
        await SeedInvoiceAsync(factory, clientId, InvoiceStatus.Paid);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.GetFromJsonAsync<PagedResponse<InvoiceSummaryDto>>($"/invoices?clientId={clientId}&status={InvoiceStatus.Paid}");

        response.ShouldNotBeNull();
        response.Items.ShouldAllBe(i => i.Status == InvoiceStatus.Paid);
        response.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Paginates_results()
    {
        await using var factory = await CreateFactoryAsync();
        var clientId = await SeedClientAsync(factory);
        for (var i = 0; i < 3; i++)
        {
            await SeedInvoiceAsync(factory, clientId, InvoiceStatus.Draft);
        }
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.GetFromJsonAsync<PagedResponse<InvoiceSummaryDto>>($"/invoices?clientId={clientId}&page=1&pageSize=2");

        response.ShouldNotBeNull();
        response.Items.Count.ShouldBe(2);
        response.TotalRecords.ShouldBe(3);
        response.TotalPages.ShouldBe(2);
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.GetAsync("/invoices");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
