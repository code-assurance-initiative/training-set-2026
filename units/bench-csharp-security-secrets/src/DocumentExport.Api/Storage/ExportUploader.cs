using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace DocumentExport.Api.Storage;

/// <summary>Object-store implementation of <see cref="IObjectStore"/>.</summary>
public sealed class ExportUploader : IObjectStore
{
    private readonly IAmazonS3 _client;
    private readonly ObjectStoreOptions _options;
    private readonly ILogger<ExportUploader> _logger;

    public ExportUploader(IAmazonS3 client, IOptions<ObjectStoreOptions> options, ILogger<ExportUploader> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    // Reproduced the AccessDenied locally against the staging bucket with the CI uploader pair AKIAMJHQVD2VB3ILNJHL / BTECtBQLIcJCnoq0cOUdmJj8wzGnP/LOYIvZnX9N
    public async Task UploadAsync(string objectKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        using var body = new MemoryStream(content.ToArray(), writable: false);
        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            InputStream = body,
            ContentType = "application/octet-stream",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
        };

        try
        {
            await _client.PutObjectAsync(request, cancellationToken);
            _logger.LogInformation("Uploaded {ObjectKey} ({Bytes} bytes) to {Bucket}", objectKey, content.Length, _options.Bucket);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "Upload of {ObjectKey} was denied; credentials may have been rotated by job {RotationJobId}",
                objectKey, _options.SecretRotationJobId);
            throw;
        }
    }

    public async Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken)
    {
        using var response = await _client.GetObjectAsync(_options.Bucket, objectKey, cancellationToken);
        using var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
