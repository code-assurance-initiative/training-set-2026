using ReportDesk.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReportDesk(builder.Configuration);

var app = builder.Build();
app.UseReportDesk();
app.Run();

/// <summary>Entry point, visible to the integration tests' <c>WebApplicationFactory</c>.</summary>
public partial class Program;
