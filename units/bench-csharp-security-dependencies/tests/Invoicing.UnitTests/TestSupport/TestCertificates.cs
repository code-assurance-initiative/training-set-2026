using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Invoicing.UnitTests.TestSupport;

public static class TestCertificates
{
    /// <summary>A throwaway self-signed signing certificate, generated in memory for the test run.</summary>
    public static X509Certificate2 CreateSigning()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Invoicing unit tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
    }
}
