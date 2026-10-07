using System.Security.Cryptography;

namespace ReportDesk.Api.Http;

/// <summary>Entity tags for rendered previews, so the front end can revalidate its cache cheaply.</summary>
public static class ETagCalculator
{
    public static string Compute(ReadOnlySpan<byte> content) =>
        $"\"{Convert.ToHexStringLower(MD5.HashData(content))}\"";
}
