using FleetOps.Application;
using FleetOps.Infrastructure;
using FleetOps.Infrastructure.Telematics;
using FleetOps.ServiceDefaults;
using FleetOps.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddFleetApplication();
builder.Services.AddFleetInfrastructure();
builder.Services.AddTelematics();
builder.Services.AddHostedService<MaintenanceReminderWorker>();

builder.Build().Run();
