using System.Collections.Concurrent;
using System.DirectoryServices.Protocols;
using ReportDesk.Api.Delivery;
using ReportDesk.Api.Documents;
using ReportDesk.Api.People;
using ReportDesk.Api.Reports;
using ReportDesk.Api.SavedSearches;
using ReportDesk.Api.Search;
using ReportDesk.Api.Shares;

namespace ReportDesk.IntegrationTests;

internal sealed class FakeArchive : IDocumentSearchRepository, IDocumentRepository
{
    public List<DocumentRecord> Documents { get; } = [];

    public Task<IReadOnlyList<DocumentHit>> SearchAsync(string term, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DocumentHit>>(Documents
            .Where(document => document.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(document => new DocumentHit(document.Id, document.Title, document.Owner, "internal", document.UpdatedAt))
            .ToList());

    public Task<DocumentRecord?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Documents.Find(document => document.Id == id));

    public Task<IReadOnlyList<DocumentRecord>> ListAsync(string? sort, bool descending, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DocumentRecord>>(Documents.Take(limit).ToList());
}

internal sealed class FakeReports : IReportRepository, IScheduleRepository, ISubscriberStore
{
    public List<ReportDefinition> Reports { get; } = [];

    public List<Subscriber> Subscribers { get; } = [];

    public Task<ReportDefinition?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Reports.Find(report => report.Id == id));

    public Task<IReadOnlyList<ReportDefinition>> ForOwnerAsync(string owner, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportDefinition>>(Reports.Where(report => report.Owner == owner).ToList());

    public Task SaveLayoutAsync(Guid id, string layoutJson, CancellationToken cancellationToken)
    {
        Reports.Single(report => report.Id == id).LayoutJson = layoutJson;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ReportSchedule>> DueForOwnerAsync(string owner, DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportSchedule>>([]);

    Task<Subscriber?> ISubscriberStore.GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Subscribers.Find(subscriber => subscriber.Id == id));

    public Task SetEmailEnabledAsync(Guid id, bool enabled, CancellationToken cancellationToken)
    {
        Subscribers.Single(subscriber => subscriber.Id == id).EmailEnabled = enabled;
        return Task.CompletedTask;
    }
}

internal sealed class FakeSavedSearches : ISavedSearchStore
{
    public ConcurrentDictionary<(string Owner, string Name), SavedSearchDefinition> Saved { get; } = new();

    public Task SaveAsync(string owner, SavedSearchDefinition search, CancellationToken cancellationToken)
    {
        Saved[(owner, search.Name)] = search;
        return Task.CompletedTask;
    }

    public Task<int> DeleteAsync(string owner, string name, CancellationToken cancellationToken) =>
        Task.FromResult(Saved.TryRemove((owner, name), out _) ? 1 : 0);
}

internal sealed class FakeShares : IShareStore
{
    public ConcurrentDictionary<string, ShareLink> Links { get; } = new();

    public Task AddAsync(ShareLink link, CancellationToken cancellationToken)
    {
        Links[link.Token] = link;
        return Task.CompletedTask;
    }

    public Task<ShareLink?> FindAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(Links.TryGetValue(token, out var link) ? link : null);

    public Task<int> PurgeExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var expired = Links.Values.Where(link => link.ExpiresAt < now).ToList();
        expired.ForEach(link => Links.TryRemove(link.Token, out _));
        return Task.FromResult(expired.Count);
    }
}

internal sealed class FakeDirectory : ILdapSearcher
{
    public List<string> Filters { get; } = [];

    public Task<IReadOnlyList<SearchResultEntry>> SearchAsync(System.DirectoryServices.Protocols.SearchRequest request, CancellationToken cancellationToken)
    {
        Filters.Add(request.Filter as string ?? string.Empty);
        return Task.FromResult<IReadOnlyList<SearchResultEntry>>([]);
    }
}

internal sealed class FakeMail : IMailTransport
{
    public ConcurrentQueue<MailMessageRequest> Sent { get; } = new();

    public Task SendAsync(MailMessageRequest message, CancellationToken cancellationToken)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }
}

/// <summary>Stands in for the CRM: knows one contact and accepts channel changes.</summary>
internal sealed class FakeCrm : HttpMessageHandler
{
    public ConcurrentQueue<string> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue($"{request.Method} {request.RequestUri?.PathAndQuery}");
        var response = request.Method == HttpMethod.Get
            ? new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"c-1","displayName":"Ann Berg","company":"Archive"}""", System.Text.Encoding.UTF8, "application/json"),
            }
            : new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
        return Task.FromResult(response);
    }
}

/// <summary>Stands in for every outbound web destination: answers 200 with a small body and records the URLs.</summary>
internal sealed class StubWeb : HttpMessageHandler
{
    public ConcurrentQueue<Uri> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null)
        {
            Requests.Enqueue(request.RequestUri);
        }

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"record\":1}]", System.Text.Encoding.UTF8, "application/json"),
        });
    }
}
