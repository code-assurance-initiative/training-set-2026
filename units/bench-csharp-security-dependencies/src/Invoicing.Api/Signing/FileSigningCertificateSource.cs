using System.Security.Cryptography.X509Certificates;
using Invoicing.Rendering.Ubl;
using Microsoft.Extensions.Options;

namespace Invoicing.Api.Signing;

/// <summary>Loads the signing certificate once, on first use, from the configured PKCS#12 file.</summary>
public sealed partial class FileSigningCertificateSource(
    IOptions<SigningCertificateOptions> options,
    ILogger<FileSigningCertificateSource> logger) : ISigningCertificateSource, IDisposable
{
    private readonly Lazy<X509Certificate2> _certificate = new(() => Load(options.Value, logger));

    public X509Certificate2 Current() => _certificate.Value;

    public void Dispose()
    {
        if (_certificate.IsValueCreated)
        {
            _certificate.Value.Dispose();
        }
    }

    private static X509Certificate2 Load(SigningCertificateOptions settings, ILogger logger)
    {
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(settings.CertificatePath, settings.Password);
        LogLoaded(logger, certificate.Subject, certificate.NotAfter);
        return certificate;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded e-invoice signing certificate {Subject}, valid until {NotAfter}")]
    private static partial void LogLoaded(ILogger logger, string subject, DateTime notAfter);
}
