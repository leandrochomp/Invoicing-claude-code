using InvoicingBff.Features.Auth;
using InvoicingBff.Features.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InvoicingBff.Tests.TestSupport;

public sealed class BffTestFactory(Func<HttpRequestMessage, HttpResponseMessage> apiResponder)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.AddHttpClient<LoginHandler>(client => client.BaseAddress = new Uri("http://invoicing-api.test"))
                .ConfigurePrimaryHttpMessageHandler(() => new FakeInvoicingApiHandler(apiResponder));

            // The auth handler is already registered by AddBffServices; these calls just swap the
            // real InvoicingApi transport for the fake one, keeping the auth-forwarding handler in place.
            services.AddHttpClient<ListClientsHandler>(client => client.BaseAddress = new Uri("http://invoicing-api.test"))
                .ConfigurePrimaryHttpMessageHandler(() => new FakeInvoicingApiHandler(apiResponder));
            services.AddHttpClient<GetClientByIdHandler>(client => client.BaseAddress = new Uri("http://invoicing-api.test"))
                .ConfigurePrimaryHttpMessageHandler(() => new FakeInvoicingApiHandler(apiResponder));
            services.AddHttpClient<CreateClientHandler>(client => client.BaseAddress = new Uri("http://invoicing-api.test"))
                .ConfigurePrimaryHttpMessageHandler(() => new FakeInvoicingApiHandler(apiResponder));
            services.AddHttpClient<UpdateClientHandler>(client => client.BaseAddress = new Uri("http://invoicing-api.test"))
                .ConfigurePrimaryHttpMessageHandler(() => new FakeInvoicingApiHandler(apiResponder));
            services.AddHttpClient<DeleteClientHandler>(client => client.BaseAddress = new Uri("http://invoicing-api.test"))
                .ConfigurePrimaryHttpMessageHandler(() => new FakeInvoicingApiHandler(apiResponder));
        });
    }
}
