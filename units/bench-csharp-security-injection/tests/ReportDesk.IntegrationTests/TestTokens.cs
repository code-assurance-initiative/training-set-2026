using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ReportDesk.IntegrationTests;

/// <summary>
/// A stand-in token issuer. Its RSA key is generated in memory for each test run and never leaves the process.
/// Tokens are stamped with real time, because token lifetimes are validated against the real clock.
/// </summary>
public sealed class TestTokens : IDisposable
{
    public const string Issuer = "https://issuer.test/";
    public const string Audience = "reportdesk-api";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly RsaSecurityKey _key;

    public TestTokens()
    {
        _key = new RsaSecurityKey(_rsa) { KeyId = "integration-tests" };
    }

    /// <summary>The issuer metadata the API would otherwise download from the authority.</summary>
    public OpenIdConnectConfiguration Metadata()
    {
        var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
        metadata.SigningKeys.Add(_key);
        return metadata;
    }

    public string Create(params string[] scopes) => Create(Audience, scopes);

    public string Create(string audience, params string[] scopes)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = "u-ann-berg",
                ["scope"] = string.Join(' ', scopes),
            },
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public void Dispose() => _rsa.Dispose();
}
