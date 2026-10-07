using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ReportDesk.Api.Delivery;
using ReportDesk.Api.Documents;
using ReportDesk.Api.Feeds;
using ReportDesk.Api.Importing;
using ReportDesk.Api.People;
using ReportDesk.Api.Reports;
using ReportDesk.Api.SavedSearches;
using ReportDesk.Api.Search;
using ReportDesk.Api.Shares;
using ReportDesk.Api.Webhooks;

namespace ReportDesk.IntegrationTests;

/// <summary>
/// Hosts the real API in memory. Every store, the directory, the mail relay and the CRM are replaced by fakes, and
/// the issuer metadata is static, so nothing leaves the process.
/// </summary>
public sealed class ReportDeskApiFactory : WebApplicationFactory<Program>
{
    internal FakeArchive Archive { get; } = new();

    internal FakeReports Reports { get; } = new();

    internal FakeSavedSearches SavedSearches { get; } = new();

    internal FakeShares Shares { get; } = new();

    internal FakeDirectory Directory { get; } = new();

    internal FakeMail Mail { get; } = new();

    internal FakeCrm Crm { get; } = new();

    public TestTokens Tokens { get; } = new();

    /// <summary>Attachments, templates, scratch space and the stand-in converters for this test run.</summary>
    public string Root { get; } = System.IO.Directory.CreateTempSubdirectory("reportdesk-it-").FullName;

    /// <summary>Answers for the imports, partner-feed and webhook clients, keyed by host.</summary>
    internal StubWeb Web { get; } = new();

    public HttpClient CreateClient(params string[] scopes)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://reportdesk.test"),
            AllowAutoRedirect = false,
        });
        if (scopes.Length > 0)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", Tokens.Create(scopes));
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Authentication:Authority", TestTokens.Issuer);
        builder.UseSetting("Authentication:Audience", TestTokens.Audience);
        builder.UseSetting("Delivery:PseudonymKey", Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("https_port", "443");
        foreach (var folder in new[] { "attachments", "templates", "scratch" })
        {
            System.IO.Directory.CreateDirectory(Path.Combine(Root, folder));
        }

        builder.UseSetting("Storage:AttachmentsRoot", Path.Combine(Root, "attachments"));
        builder.UseSetting("Storage:TemplatesRoot", Path.Combine(Root, "templates"));
        builder.UseSetting("Storage:ScratchRoot", Path.Combine(Root, "scratch"));
        builder.UseSetting("Conversion:SofficePath", FakeTools.Write(Root, "soffice", FakeTools.Soffice));
        builder.UseSetting("Conversion:ConvertPath", FakeTools.Write(Root, "convert", FakeTools.Convert));
        builder.UseSetting("Feeds:AllowedHosts:0", "records.partner.test");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDocumentSearchRepository>().AddSingleton<IDocumentSearchRepository>(Archive);
            services.RemoveAll<IDocumentRepository>().AddSingleton<IDocumentRepository>(Archive);
            services.RemoveAll<IReportRepository>().AddSingleton<IReportRepository>(Reports);
            services.RemoveAll<IScheduleRepository>().AddSingleton<IScheduleRepository>(Reports);
            services.RemoveAll<ISubscriberStore>().AddSingleton<ISubscriberStore>(Reports);
            services.RemoveAll<ISavedSearchStore>().AddSingleton<ISavedSearchStore>(SavedSearches);
            services.RemoveAll<IShareStore>().AddSingleton<IShareStore>(Shares);
            services.RemoveAll<ILdapSearcher>().AddSingleton<ILdapSearcher>(Directory);
            services.RemoveAll<IMailTransport>().AddSingleton<IMailTransport>(Mail);
            services.AddHttpClient<SubscriberDirectoryClient>().ConfigurePrimaryHttpMessageHandler(() => Crm);
            services.AddHttpClient<PartnerFeedClient>().ConfigurePrimaryHttpMessageHandler(() => Web);
            services.AddHttpClient<WebhookDispatcher>().ConfigurePrimaryHttpMessageHandler(() => Web);
            services.AddHttpClient(ImportClient.Name).ConfigurePrimaryHttpMessageHandler(() => Web);
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var metadata = Tokens.Metadata();
                options.Configuration = metadata;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Tokens.Dispose();
            Crm.Dispose();
            Web.Dispose();
            System.IO.Directory.Delete(Root, recursive: true);
        }

        base.Dispose(disposing);
    }
}
