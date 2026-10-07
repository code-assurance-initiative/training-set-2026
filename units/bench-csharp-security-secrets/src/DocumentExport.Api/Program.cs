using DocumentExport.Api.Encryption;
using DocumentExport.Api.Exports;
using DocumentExport.Api.Http;
using DocumentExport.Api.Notifications;
using DocumentExport.Api.Partner;
using DocumentExport.Api.Persistence;
using DocumentExport.Api.Signing;
using DocumentExport.Api.Storage;
using DocumentExport.Api.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// partner-api.json and an optional appsettings.Local.json sit between appsettings and the environment, so
// environment variables and the command line still override them.
builder.Configuration
    .AddJsonFile("partner-api.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddOptions<ObjectStoreOptions>().BindConfiguration(ObjectStoreOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<DownloadTokenOptions>().BindConfiguration(DownloadTokenOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<PartnerOptions>().BindConfiguration(PartnerOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<NotificationOptions>().BindConfiguration(NotificationOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<AuditStoreSettings>().BindConfiguration(AuditStoreSettings.SectionName).ValidateDataAnnotations();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => ObjectStoreClientFactory.Create(sp.GetRequiredService<IOptions<ObjectStoreOptions>>()));
builder.Services.AddSingleton<IObjectStore, ExportUploader>();
builder.Services.AddSingleton(sp => ExportsDataSourceFactory.Create(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<IExportStore, ExportStore>();
builder.Services.AddSingleton<IAuditLog, AuditLog>();
builder.Services.AddSingleton<ExportEncryptor>();
builder.Services.AddSingleton(_ => new ManifestSigner(
    Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Signing:PrivateKeyPath"] ?? "Keys/export-signing.pem")));
builder.Services.AddSingleton<DownloadTokenService>();
builder.Services.AddScoped<ExportService>();

builder.Services.AddHttpClient<IPartnerApi, PartnerApiClient>((sp, client) =>
    {
        var partner = sp.GetRequiredService<IOptions<PartnerOptions>>().Value;
        client.BaseAddress = partner.BaseUrl;
        client.Timeout = partner.Timeout;
        foreach (var (name, value) in partner.DefaultRequestHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
        }
    })
    .AddStandardResilienceHandler();
builder.Services.AddHttpClient<IDeliveryNotifier, DeliveryNotifier>().AddStandardResilienceHandler();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var identity = builder.Configuration.GetSection("Identity");
        options.Authority = $"{identity["Instance"]}{identity["TenantId"]}/v2.0";
        options.Audience = identity["ClientId"];
        options.MapInboundClaims = false;
        options.TokenValidationParameters.RoleClaimType = "roles";
    })
    .AddScheme<AuthenticationSchemeOptions, DownloadTokenAuthenticationHandler>(DownloadTokenAuthenticationHandler.SchemeName, null);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ExportPolicies.Read, policy => policy.RequireRole("Exports.Read", "Exports.Write"))
    .AddPolicy(ExportPolicies.Write, policy => policy.RequireRole("Exports.Write"))
    .AddPolicy(ExportPolicies.Download, policy => policy
        .AddAuthenticationSchemes(DownloadTokenAuthenticationHandler.SchemeName)
        .RequireClaim(DownloadTokenService.ExportIdClaim));

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapExportEndpoints();

app.Logger.LogInformation("Document export service starting in {Environment}", app.Environment.EnvironmentName);
await app.RunAsync();

/// <summary>Entry point; public so integration tests can host the application.</summary>
public partial class Program;
