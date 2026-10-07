using Fx.Conversion.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddConversionApi(builder.Environment);

var app = builder.Build();
app.UseConversionApi();
app.Run();

/// <summary>The entry point, visible to the in-memory test host.</summary>
public partial class Program;
