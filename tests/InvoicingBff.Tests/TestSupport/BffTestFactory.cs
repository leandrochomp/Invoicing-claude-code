using InvoicingBff.Features.Auth;
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
        });
    }
}
