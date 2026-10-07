namespace DocumentExport.Api.Storage;

/// <summary>Stores and retrieves encrypted export blobs.</summary>
public interface IObjectStore
{
    Task UploadAsync(string objectKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);

    Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken);
}
