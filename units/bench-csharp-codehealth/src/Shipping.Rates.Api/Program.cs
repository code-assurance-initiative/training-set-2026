using Shipping.Rates.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddShippingApi(builder.Configuration);

var app = builder.Build();
app.UseShippingApi();
app.Run();

/// <summary>Entry point; public so the integration tests can host the API.</summary>
public partial class Program;
