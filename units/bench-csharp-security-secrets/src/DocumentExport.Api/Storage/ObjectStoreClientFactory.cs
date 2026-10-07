using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace DocumentExport.Api.Storage;

/// <summary>
/// Creates the S3-compatible client for the export bucket. The configured access key id has the vendor's documented
/// shape (for instance AKIAIOSFODNN7EXAMPLE); the client never logs it.
/// </summary>
public static class ObjectStoreClientFactory
{
    public static IAmazonS3 Create(IOptions<ObjectStoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var settings = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = settings.ServiceUrl,
            AuthenticationRegion = settings.Region,
            ForcePathStyle = true,
            Timeout = TimeSpan.FromSeconds(30),
            MaxErrorRetry = 3,
        };

        return new AmazonS3Client(new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey), config);
    }
}
