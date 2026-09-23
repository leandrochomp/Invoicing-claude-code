using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace InvoicingBff.Tests.TestSupport;

public sealed class BffTestFactory(Func<HttpRequestMessage, HttpResponseMessage> apiResponder)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Swaps the real InvoicingApi transport for the fake one on every typed client AddBffServices
        // registers, keeping each client's auth-forwarding handler in place.
        builder.ConfigureServices(services =>
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
                options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                    handlerBuilder.PrimaryHandler = new FakeInvoicingApiHandler(apiResponder))));
    }
}
