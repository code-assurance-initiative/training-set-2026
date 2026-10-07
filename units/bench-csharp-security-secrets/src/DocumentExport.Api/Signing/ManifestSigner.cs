using System.Security.Cryptography;

namespace DocumentExport.Api.Signing;

/// <summary>A signed statement of what an export contains.</summary>
/// <param name="Sha256">Hex SHA-256 of the rendered (unencrypted) document.</param>
/// <param name="Signature">Base64 RSA-PSS/SHA-256 signature over the document.</param>
public sealed record ExportManifest(string Sha256, string Signature);

/// <summary>Signs export manifests with the service's RSA key so consumers can verify what they downloaded.</summary>
public sealed class ManifestSigner : IDisposable
{
    private readonly RSA _rsa;

    public ManifestSigner(string privateKeyPemPath)
    {
        _rsa = RSA.Create();
        _rsa.ImportFromPem(File.ReadAllText(privateKeyPemPath));
    }

    public ExportManifest Sign(ReadOnlySpan<byte> document)
    {
        var digest = SHA256.HashData(document);
        var signature = _rsa.SignHash(digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return new ExportManifest(Convert.ToHexStringLower(digest), Convert.ToBase64String(signature));
    }

    public void Dispose() => _rsa.Dispose();
}
