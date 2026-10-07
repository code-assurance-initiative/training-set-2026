namespace FleetOps.Infrastructure.Storage;

public interface IDocumentStore
{
    Task<StoredDocument> PutAsync(DocumentKind kind, string name, Stream content, CancellationToken cancellationToken);

    Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken);
}
