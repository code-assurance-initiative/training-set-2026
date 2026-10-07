using Microsoft.AspNetCore.Authorization;

namespace Depot.Slots.Api.Security;

/// <summary>The named policies, each granted by one OAuth scope in the caller's access token.</summary>
public static class AuthorizationPolicies
{
    public const string SlotsRead = "slots.read";
    public const string SlotsWrite = "slots.write";

    public static AuthorizationBuilder AddSlotsPolicies(this AuthorizationBuilder builder) =>
        builder
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(SlotsRead, policy => policy.RequireScope(SlotsRead))
            .AddPolicy(SlotsWrite, policy => policy.RequireScope(SlotsWrite));

    private static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
