using Depot.Slots.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSlotsApi(builder.Configuration);

var app = builder.Build();
app.UseSlotsApi();
app.Run();
