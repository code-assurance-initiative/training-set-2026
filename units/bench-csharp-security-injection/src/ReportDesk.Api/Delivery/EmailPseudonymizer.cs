using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ReportDesk.Api.Delivery;

/// <summary>A stable, keyed pseudonym for an e-mail address, for logs and metrics that must not hold the address.</summary>
public sealed class EmailPseudonymizer(IOptions<DeliveryOptions> options)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.Value.PseudonymKey);

    public string Hash(string emailAddress)
    {
        ArgumentNullException.ThrowIfNull(emailAddress);
        var normalized = Encoding.UTF8.GetBytes(emailAddress.Trim().ToUpperInvariant());
        return Convert.ToHexStringLower(HMACSHA256.HashData(_key, normalized))[..16];
    }
}
