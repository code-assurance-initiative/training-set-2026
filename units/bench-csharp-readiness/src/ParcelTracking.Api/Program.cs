using ParcelTracking.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddParcelTrackingApi();

var app = builder.Build();
app.UseParcelTrackingApi();
await app.RunAsync();

/// <summary>Entry point; public so the integration tests can host the API.</summary>
public partial class Program;
