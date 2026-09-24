using System.Net;
using System.Net.Http.Json;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class SendInvoiceEndpointTests(PostgresFixture postgres)
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

    private static async Task<Invoice> SeedInvoiceAsync(WebApplicationFactory<Program> factory, InvoiceStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        return await InvoiceHandlerTestData.SeedInvoiceAsync(context, status);
    }

    [Fact]
    public async Task Sends_draft_invoice()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory, InvoiceStatus.Draft);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{invoice.Id}/send", new SendInvoiceRequest(invoice.Version));
        var body = await response.Content.ReadFromJsonAsync<InvoiceDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task Returns_conflict_when_already_sent()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory, InvoiceStatus.Sent);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{invoice.Id}/send", new SendInvoiceRequest(invoice.Version));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_invoice()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{Guid.NewGuid()}/send", new SendInvoiceRequest(0));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_validation_problem_for_negative_version()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{Guid.NewGuid()}/send", new SendInvoiceRequest(-1));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.PostAsJsonAsync($"/invoices/{Guid.NewGuid()}/send", new SendInvoiceRequest(0));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
