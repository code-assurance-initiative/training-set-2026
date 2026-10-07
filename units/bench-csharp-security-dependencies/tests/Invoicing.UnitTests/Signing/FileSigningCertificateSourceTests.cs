using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Invoicing.Api.Signing;
using Invoicing.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Invoicing.UnitTests.Signing;

public sealed class FileSigningCertificateSourceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"signing-{Guid.NewGuid():N}.p12");

    [Fact]
    public void LoadsTheCertificateWithItsPrivateKeyOnce()
    {
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        using (var generated = TestCertificates.CreateSigning())
        {
            File.WriteAllBytes(_path, generated.Export(X509ContentType.Pkcs12, password));
        }

        using var source = new FileSigningCertificateSource(
            Options.Create(new SigningCertificateOptions { CertificatePath = _path, Password = password }),
            NullLogger<FileSigningCertificateSource>.Instance);

        var certificate = source.Current();

        certificate.HasPrivateKey.Should().BeTrue();
        certificate.Subject.Should().Be("CN=Invoicing unit tests");
        source.Current().Should().BeSameAs(certificate);
    }

    [Fact]
    public void AWrongPasswordFailsOnFirstUse()
    {
        using (var generated = TestCertificates.CreateSigning())
        {
            File.WriteAllBytes(_path, generated.Export(X509ContentType.Pkcs12, "right"));
        }

        using var source = new FileSigningCertificateSource(
            Options.Create(new SigningCertificateOptions { CertificatePath = _path, Password = "wrong" }),
            NullLogger<FileSigningCertificateSource>.Instance);

        var act = () => source.Current();

        act.Should().Throw<CryptographicException>();
    }

    public void Dispose() => File.Delete(_path);
}
