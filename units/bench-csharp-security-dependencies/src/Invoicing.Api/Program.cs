using Invoicing.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInvoicingApi();

var app = builder.Build();
app.UseInvoicingApi();
app.Run();

