using System.Security.Claims;

namespace Warehouse.Stock.Api.Security;

public static class ScopeClaims
{
    /// <summary>The OAuth 2.0 access-token claim (RFC 8693 §4.2): one space-delimited string of scopes.</summary>
    public const string ClaimType = "scope";

    public static bool HasScope(ClaimsPrincipal user, string scope)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user
            .FindAll(ClaimType)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope, StringComparer.Ordinal);
    }
}
