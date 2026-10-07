using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReportDesk.Api.Attachments;
using ReportDesk.Api.Conversion;
using ReportDesk.Api.Delivery;
using ReportDesk.Api.Documents;
using ReportDesk.Api.Feeds;
using ReportDesk.Api.Http;
using ReportDesk.Api.Importing;
using ReportDesk.Api.Metadata;
using ReportDesk.Api.People;
using ReportDesk.Api.Previews;
using ReportDesk.Api.Reports;
using ReportDesk.Api.SavedSearches;
using ReportDesk.Api.Search;
using ReportDesk.Api.Security;
using ReportDesk.Api.Shares;
using ReportDesk.Api.Templates;
using ReportDesk.Api.Webhooks;

namespace ReportDesk.Api.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportDesk(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(HtmlEncoder.Default);

        services.AddReportDeskOptions();
        services.AddReportDeskData(configuration);
        services.AddReportDeskHttpClients();
        services.AddReportDeskFeatures();
        return services.AddReportDeskSecurity();
    }

    private static void AddReportDeskOptions(this IServiceCollection services)
    {
        services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ConversionOptions>().BindConfiguration(ConversionOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<FeedOptions>().BindConfiguration(FeedOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<PeopleDirectoryOptions>().BindConfiguration(PeopleDirectoryOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<DeliveryOptions>().BindConfiguration(DeliveryOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
    }

    private static void AddReportDeskData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Archive") ?? string.Empty;
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddDbContext<ReportsDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IDocumentSearchRepository, DocumentSearchRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<ISubscriberStore, SubscriberStore>();
        services.AddScoped<ISavedSearchStore, SavedSearchStore>();
        services.AddScoped<IShareStore, ShareStore>();
    }

    private static void AddReportDeskHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(ImportClient.Name, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.MaxResponseContentBufferSize = ImportClient.MaxResponseBytes;
        });

        services.AddHttpClient<PartnerFeedClient>()
            .ConfigurePrimaryHttpMessageHandler(() => GuardedConnect.CreateHandler(FeedAddressPolicy.IsPublicIPv4Destination))
            .AddStandardResilienceHandler();

        services.AddHttpClient<WebhookDispatcher>(client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => GuardedConnect.CreateHandler(CallbackAddressPolicy.IsPublic));

        services.AddHttpClient<SubscriberDirectoryClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DeliveryOptions>>().Value;
            client.BaseAddress = options.CrmBaseAddress;
        }).AddStandardResilienceHandler();
    }

    private static void AddReportDeskFeatures(this IServiceCollection services)
    {
        services.AddSingleton<HighlightService>();
        services.AddSingleton<AttachmentStore>();
        services.AddSingleton<TemplateStore>();
        services.AddSingleton<DocumentConverter>();
        services.AddSingleton<ThumbnailRenderer>();
        services.AddSingleton<MetadataImporter>();
        services.AddSingleton<DocumentCardRenderer>();
        services.AddSingleton<ILdapSearcher, LdapSearcher>();
        services.AddSingleton<OwnerDirectory>();
        services.AddSingleton<GroupDirectory>();
        services.AddSingleton<IMailTransport, SmtpMailTransport>();
        services.AddSingleton<EmailPseudonymizer>();
        services.AddScoped<ReportMailer>();
        services.AddSingleton<ShareLinkCleanupJob>();
        services.AddHostedService(provider => provider.GetRequiredService<ShareLinkCleanupJob>());
    }

    private static IServiceCollection AddReportDeskSecurity(this IServiceCollection services)
    {
        services
            .AddOptions<JwtAuthenticationOptions>()
            .BindConfiguration(JwtAuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasHttpsAuthority(), "Authentication:Authority must be an absolute https URL.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddReportDeskPolicies();
        return services;
    }
}
