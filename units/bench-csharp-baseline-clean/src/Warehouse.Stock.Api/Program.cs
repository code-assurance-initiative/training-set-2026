using Warehouse.Stock.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddStockApi();

var app = builder.Build();
app.UseStockApi();
app.Run();
