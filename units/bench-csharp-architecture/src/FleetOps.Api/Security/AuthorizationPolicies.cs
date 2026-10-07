using Microsoft.AspNetCore.Authorization;

namespace FleetOps.Api.Security;

/// <summary>The named policies, each granted by one OAuth scope in the caller's access token.</summary>
public static class AuthorizationPolicies
{
    public const string FleetRead = "fleet.read";
    public const string FleetWrite = "fleet.write";
    public const string FleetManage = "fleet.manage";

    public static AuthorizationBuilder AddFleetPolicies(this AuthorizationBuilder builder) =>
        builder
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(FleetRead, policy => policy.RequireScope(FleetRead))
            .AddPolicy(FleetWrite, policy => policy.RequireScope(FleetWrite))
            .AddPolicy(FleetManage, policy => policy.RequireScope(FleetManage));

    private static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
