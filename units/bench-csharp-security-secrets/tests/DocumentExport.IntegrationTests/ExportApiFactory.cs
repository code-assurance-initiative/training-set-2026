using DocumentExport.Api.Notifications;
using DocumentExport.Api.Partner;
using DocumentExport.Api.Persistence;
using DocumentExport.Api.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentExport.IntegrationTests;

/// <summary>Hosts the API with in-memory stores and a header-driven test identity.</summary>
public sealed class ExportApiFactory : WebApplicationFactory<Program>
{
    internal RecordingAuditLog Audit { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config =>
            config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "fixtures", "objectstore.json"), optional: false));

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IObjectStore, InMemoryObjectStore>();
            services.AddSingleton<IExportStore, InMemoryExportStore>();
            services.AddSingleton<IAuditLog>(Audit);
            services.AddSingleton<SilentPartner>();
            services.AddSingleton<IPartnerApi>(sp => sp.GetRequiredService<SilentPartner>());
            services.AddSingleton<IDeliveryNotifier>(sp => sp.GetRequiredService<SilentPartner>());

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, null);
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
            });
        });
    }
}
