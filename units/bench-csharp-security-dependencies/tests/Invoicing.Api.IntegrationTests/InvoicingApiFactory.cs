using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Invoicing.Api.Branding;
using Invoicing.Rendering.Ubl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Invoicing.Api.IntegrationTests;

/// <summary>
/// Hosts the real API in memory. Replaced: the issuer metadata (tokens from <see cref="TestTokens"/> validate
/// without a network call), the branding service (a fixed logo) and the signing certificate (generated per run).
/// </summary>
public sealed class InvoicingApiFactory : WebApplicationFactory<Program>
{
    public TestTokens Tokens { get; } = new();

    public X509Certificate2 SigningCertificate { get; } = CreateCertificate();

    public HttpClient CreateClient(params string[] scopes)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://invoicing.test") });
        if (scopes.Length > 0)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", Tokens.Create(scopes));
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Authentication:Authority", TestTokens.Issuer);
        builder.UseSetting("Authentication:Audience", TestTokens.Audience);
        builder.UseSetting("Branding:BaseAddress", "https://branding.test/");
        builder.UseSetting("Branding:FooterText", "Nordlys Software ApS · Havnegade 4 · Aarhus");
        builder.UseSetting("https_port", "443");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILogoSource>();
            services.AddSingleton<ILogoSource, NoLogos>();
            services.RemoveAll<ISigningCertificateSource>();
            services.AddSingleton<ISigningCertificateSource>(new FixedCertificate(SigningCertificate));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var metadata = Tokens.Metadata();
                options.Configuration = metadata;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Tokens.Dispose();
            SigningCertificate.Dispose();
        }

        base.Dispose(disposing);
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Invoicing integration tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
    }

    private sealed class NoLogos : ILogoSource
    {
        public Task<byte[]?> GetLogoAsync(string tenant, CancellationToken cancellationToken) => Task.FromResult<byte[]?>(null);
    }

    private sealed class FixedCertificate(X509Certificate2 certificate) : ISigningCertificateSource
    {
        public X509Certificate2 Current() => certificate;
    }
}
