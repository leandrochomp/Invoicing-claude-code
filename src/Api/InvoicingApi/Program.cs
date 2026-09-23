using InvoicingApi.Extensions;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Dashboard;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Users;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiServices();

var app = builder.Build();

app.ConfigureApi();

app.MapRegisterUserEndpoint();
app.MapLoginEndpoint();

app.MapClientEndpoints();
app.MapCreateClientEndpoint();
app.MapUpdateClientEndpoint();
app.MapDeleteClientEndpoint();
app.MapListClientsEndpoint();
app.MapInvoiceEndpoints();
app.MapPaymentEndpoints();
app.MapGetDashboardSummaryEndpoint();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/throw", IResult () => throw new InvalidOperationException("Deliberate test exception."));
}

app.Run();
