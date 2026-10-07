using System.Security.Claims;

namespace ParcelTracking.Api.Security;

public static class ScopeClaims
{
    /// <summary>The OAuth 2.0 access-token claim (RFC 8693 §4.2): one space-delimited string of scopes.</summary>
    public const string ScopeClaimType = "scope";

    /// <summary>The merchant the caller acts for; every parcel query is scoped to it.</summary>
    public const string MerchantClaimType = "merchant_id";

    public static bool HasScope(ClaimsPrincipal user, string scope)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user
            .FindAll(ScopeClaimType)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope, StringComparer.Ordinal);
    }

    public static string MerchantId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirstValue(MerchantClaimType)
            ?? throw new InvalidOperationException("The authorization policy admitted a caller without a merchant_id claim.");
    }
}
