using HarbourLane.Bookings.Web.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBookingPortal(builder.Configuration);

var app = builder.Build();
app.UseBookingPortal();
app.Run();

/// <summary>Entry point, public so integration tests can host the application.</summary>
public partial class Program;
