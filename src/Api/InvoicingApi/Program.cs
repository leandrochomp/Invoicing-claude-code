using InvoicingApi.Extensions;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiServices();

var app = builder.Build();

app.ConfigureApi();

app.MapClientEndpoints();
app.MapInvoiceEndpoints();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/throw", IResult () => throw new InvalidOperationException("Deliberate test exception."));
}

app.Run();
