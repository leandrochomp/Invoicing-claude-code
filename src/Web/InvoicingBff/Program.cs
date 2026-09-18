using InvoicingBff.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddBffServices();

var app = builder.Build();

app.ConfigureBff();

app.Run();
