using System.Text.Json.Serialization;
using FleetOps.Api.Errors;
using FleetOps.Api.Security;
using FleetOps.Application;
using FleetOps.Infrastructure;
using FleetOps.Infrastructure.Telematics;
using FleetOps.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddFleetApplication();
builder.Services.AddFleetInfrastructure();
builder.Services.AddTelematics();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
builder.Services
    .AddOptions<JwtAuthenticationOptions>()
    .BindConfiguration(JwtAuthenticationOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(options => options.HasHttpsAuthority(), "Authentication:Authority must be an absolute https URL.")
    .ValidateOnStart();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.ConfigureOptions<ConfigureJwtBearerOptions>();
builder.Services.AddAuthorizationBuilder().AddFleetPolicies();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.Run();

/// <summary>Entry point; public so the integration tests can host the API.</summary>
public partial class Program;
