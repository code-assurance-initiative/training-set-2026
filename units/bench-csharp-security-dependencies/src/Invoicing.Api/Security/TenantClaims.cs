using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Invoicing.Api.Security;

public static partial class TenantClaims
{
    public const string ClaimType = "tenant";

    /// <summary>The caller's tenant slug, or null when the token carries none or an invalid one.</summary>
    public static string? TenantOf(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var value = user.FindFirst(ClaimType)?.Value;
        return value is not null && Slug().IsMatch(value) ? value : null;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,38}[a-z0-9]$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex Slug();
}
