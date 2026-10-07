using System.Security.Cryptography;
using System.Text;

namespace DocumentExport.Api.Encryption;

/// <summary>
/// Encrypts exports at rest with AES-256-GCM. The output is <c>nonce (12) | tag (16) | ciphertext</c>.
/// </summary>
public sealed class ExportEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static readonly byte[] Key = Encoding.UTF8.GetBytes("lpQnmizujVzcItD7^kDp#S6X-#ZlzLxL");

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext)
    {
        var output = new byte[NonceSize + TagSize + plaintext.Length];
        var nonce = output.AsSpan(0, NonceSize);
        var tag = output.AsSpan(NonceSize, TagSize);
        var ciphertext = output.AsSpan(NonceSize + TagSize);

        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(Key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return output;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("The payload is too short to be an encrypted export.");
        }

        var nonce = payload[..NonceSize];
        var tag = payload.Slice(NonceSize, TagSize);
        var ciphertext = payload[(NonceSize + TagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(Key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
