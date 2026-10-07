using System.Security.Cryptography;
using System.Text;
using DocumentExport.Api.Encryption;

namespace DocumentExport.UnitTests.Encryption;

public sealed class ExportEncryptorTests
{
    private readonly ExportEncryptor _encryptor = new();

    [Fact]
    public void Decrypt_returns_what_was_encrypted()
    {
        var document = Encoding.UTF8.GetBytes("sku,description,quantity,bin_location\r\nA-100,Pallet wrap,12,R01-S2\r\n");

        var payload = _encryptor.Encrypt(document);

        Assert.Equal(document, _encryptor.Decrypt(payload));
    }

    [Fact]
    public void Each_encryption_uses_a_fresh_nonce()
    {
        var document = Encoding.UTF8.GetBytes("same document");

        Assert.NotEqual(_encryptor.Encrypt(document), _encryptor.Encrypt(document));
    }

    [Fact]
    public void Decrypt_rejects_a_tampered_payload()
    {
        var payload = _encryptor.Encrypt(Encoding.UTF8.GetBytes("quantity,12"));
        payload[^1] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() => _encryptor.Decrypt(payload));
    }

    [Fact]
    public void Decrypt_rejects_a_truncated_payload()
    {
        Assert.Throws<CryptographicException>(() => _encryptor.Decrypt(new byte[10]));
    }
}
