using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ParcelTracking.IntegrationTests;

/// <summary>
/// A stand-in token issuer. Its RSA key is generated in memory for each test run and never leaves the process.
/// </summary>
public sealed class TestTokens : IDisposable
{
    public const string Issuer = "https://issuer.test/";
    public const string Audience = "parcel-tracking-api";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly RsaSecurityKey _key;

    public TestTokens()
    {
        _key = new RsaSecurityKey(_rsa) { KeyId = "integration-tests" };
    }

    public OpenIdConnectConfiguration Metadata()
    {
        var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
        metadata.SigningKeys.Add(_key);
        return metadata;
    }

    public string Create(string merchantId, params string[] scopes)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = $"{merchantId}-integration",
                ["merchant_id"] = merchantId,
                ["scope"] = string.Join(' ', scopes),
            },
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public void Dispose() => _rsa.Dispose();
}
