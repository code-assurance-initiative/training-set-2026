using Microsoft.Extensions.Options;
using ReportDesk.Api.Hosting;

namespace ReportDesk.Api.Attachments;

/// <summary>Attachments are stored on disk as <c>{root}/{documentId:N}/{fileName}</c>.</summary>
public sealed class AttachmentStore(IOptions<StorageOptions> storage)
{
    private readonly string _root = storage.Value.AttachmentsRoot;

    public async Task<byte[]?> ReadAsync(Guid documentId, string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_root, documentId.ToString("N"), fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }
}
