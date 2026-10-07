using Microsoft.AspNetCore.Authorization;

namespace ParcelTracking.Api.Security;

/// <summary>The named policies: each requires a merchant identity and one OAuth scope.</summary>
public static class AuthorizationPolicies
{
    public const string ReadParcels = "parcels:read";
    public const string WriteParcels = "parcels:write";
    public const string ReadReports = "reports:read";

    public static AuthorizationBuilder AddParcelTrackingPolicies(this AuthorizationBuilder builder) =>
        builder
            .AddPolicy(ReadParcels, policy => policy.RequireMerchantScope(ReadParcels))
            .AddPolicy(WriteParcels, policy => policy.RequireMerchantScope(WriteParcels))
            .AddPolicy(ReadReports, policy => policy.RequireMerchantScope(ReadReports));

    private static AuthorizationPolicyBuilder RequireMerchantScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireClaim(ScopeClaims.MerchantClaimType)
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
