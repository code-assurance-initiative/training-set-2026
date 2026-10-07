using System.Security.Cryptography.X509Certificates;

namespace Invoicing.Rendering.Ubl;

/// <summary>Supplies the e-invoice signing certificate (with its private key) from wherever the host keeps it.</summary>
public interface ISigningCertificateSource
{
    X509Certificate2 Current();
}
