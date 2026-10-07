using System.Security.Cryptography;
using System.Text;

namespace ReportDesk.Api.Shares;

/// <summary>Hashes and checks the passwords that protect share links.</summary>
public static class SharePasswordHasher
{
    public static string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(password)));
    }

    public static bool Verify(string password, string storedHash)
    {
        ArgumentNullException.ThrowIfNull(storedHash);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(password)),
            Encoding.ASCII.GetBytes(storedHash));
    }
}
