using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Invoicing.Rendering.Ubl;

/// <summary>
/// Adds an enveloped XML-DSig signature (RSA-SHA256, exclusive canonicalisation) over the whole e-invoice, with the
/// signing certificate in <c>KeyInfo</c> so the receiving gateway can check it against its trust list.
/// </summary>
public sealed partial class XmlInvoiceSigner(ISigningCertificateSource certificates, ILogger<XmlInvoiceSigner> logger)
{
    public XmlDocument Sign(XmlDocument invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var certificate = certificates.Current();
        using var key = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("The signing certificate has no RSA private key.");

        var signed = (XmlDocument)invoice.Clone();
        var root = signed.DocumentElement ?? throw new ArgumentException("The invoice document is empty.", nameof(invoice));
        var signedXml = new SignedXml(signed) { SigningKey = key };
        var signedInfo = signedXml.SignedInfo ?? throw new InvalidOperationException("SignedXml created no SignedInfo.");
        signedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
        signedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

        var reference = new Reference(string.Empty) { DigestMethod = SignedXml.XmlDsigSHA256Url };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificate));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();
        root.AppendChild(signed.ImportNode(signedXml.GetXml(), deep: true));
        LogSigned(certificate.Thumbprint);
        return signed;
    }

    /// <summary>True when the document carries exactly one signature that verifies against <paramref name="expected"/>.</summary>
    public static bool Verify(XmlDocument document, X509Certificate2 expected)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(expected);

        var signatures = document.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
        if (signatures.Count != 1 || signatures[0] is not XmlElement signature)
        {
            return false;
        }

        var signedXml = new SignedXml(document);
        signedXml.LoadXml(signature);
        using RSA? publicKey = expected.GetRSAPublicKey();
        return publicKey is not null && signedXml.CheckSignature(publicKey);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Signed e-invoice with certificate {Thumbprint}")]
    private partial void LogSigned(string thumbprint);
}
