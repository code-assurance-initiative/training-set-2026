using System.Security.Cryptography;
using System.Text;
using DocumentExport.Api.Signing;

namespace DocumentExport.UnitTests.Signing;

public sealed class ManifestSignerTests
{
    private static readonly string KeyDirectory = Path.Combine(AppContext.BaseDirectory, "Keys");

    [Fact]
    public void The_manifest_digest_is_the_sha256_of_the_document()
    {
        using var signer = new ManifestSigner(Path.Combine(KeyDirectory, "export-signing.pem"));

        var manifest = signer.Sign(Encoding.UTF8.GetBytes("abc"));

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", manifest.Sha256);
    }

    [Fact]
    public void The_signature_verifies_with_the_published_public_key()
    {
        using var signer = new ManifestSigner(Path.Combine(KeyDirectory, "export-signing.pem"));
        var document = Encoding.UTF8.GetBytes("sku,description,quantity,bin_location\r\n");

        var manifest = signer.Sign(document);

        using var publicKey = RSA.Create();
        publicKey.ImportFromPem(File.ReadAllText(Path.Combine(KeyDirectory, "export-signing.pub.pem")));
        Assert.True(publicKey.VerifyData(document, Convert.FromBase64String(manifest.Signature),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
    }
}
