using Amazon.S3;
using DocumentExport.Api.Storage;
using Microsoft.Extensions.Options;

namespace DocumentExport.UnitTests.Storage;

public sealed class ObjectStoreClientFactoryTests
{
    [Fact]
    public void Client_uses_path_style_addressing_against_the_configured_endpoint()
    {
        var options = Options.Create(new ObjectStoreOptions
        {
            ServiceUrl = "https://objects.test.internal",
            Bucket = "exports-under-test",
            Region = "eu-north-1",
            AccessKeyId = FakeCredentials.AccessKeyId,
            SecretAccessKey = FakeCredentials.SecretAccessKey,
        });

        using var client = ObjectStoreClientFactory.Create(options);

        var config = Assert.IsType<AmazonS3Config>(client.Config);
        Assert.True(config.ForcePathStyle);
        Assert.Equal("https://objects.test.internal/", config.ServiceURL);
        Assert.Equal("eu-north-1", config.AuthenticationRegion);
        Assert.Equal(3, config.MaxErrorRetry);
    }
}
