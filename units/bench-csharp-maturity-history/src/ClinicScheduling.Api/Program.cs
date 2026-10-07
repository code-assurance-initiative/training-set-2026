using ClinicScheduling.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSchedulingApi();

var app = builder.Build();
app.UseSchedulingApi();
app.Run();

/// <summary>Entry point, public so the integration tests can host it.</summary>
public partial class Program;
