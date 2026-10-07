using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure.Storage;

/// <summary>Stores documents under a root directory, one folder per kind. Keys never contain caller path segments.</summary>
public sealed class FileSystemDocumentStore(IOptions<StorageOptions> options, TimeProvider clock) : IDocumentStore
{
    public async Task<StoredDocument> PutAsync(DocumentKind kind, string name, Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var key = $"{kind}/{Guid.NewGuid():N}{Path.GetExtension(Path.GetFileName(name))}";
        var path = PathOf(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? options.Value.RootDirectory);
        var file = File.Create(path);
        await using (file.ConfigureAwait(false))
        {
            await content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            return new StoredDocument(key, kind, file.Length, clock.GetUtcNow());
        }
    }

    public Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken)
    {
        var path = PathOf(key);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    private string PathOf(string key)
    {
        var root = Path.GetFullPath(options.Value.RootDirectory);
        var full = Path.GetFullPath(Path.Combine(root, key));
        return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? full
            : throw new ArgumentException($"'{key}' is not a document key.", nameof(key));
    }
}
