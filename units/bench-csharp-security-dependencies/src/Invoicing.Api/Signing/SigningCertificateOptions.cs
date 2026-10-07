using System.ComponentModel.DataAnnotations;

namespace Invoicing.Api.Signing;

/// <summary>
/// The PKCS#12 file with the e-invoice signing certificate. The installer mounts it; its password comes from the
/// environment (<c>Signing__Password</c>), never from a committed file.
/// </summary>
public sealed class SigningCertificateOptions
{
    public const string SectionName = "Signing";

    [Required]
    public string CertificatePath { get; set; } = string.Empty;

    public string? Password { get; set; }
}
