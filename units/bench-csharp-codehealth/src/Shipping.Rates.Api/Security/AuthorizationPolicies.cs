using Microsoft.AspNetCore.Authorization;

namespace Shipping.Rates.Api.Security;

/// <summary>The named policies, each granted by one OAuth scope in the caller's access token.</summary>
public static class AuthorizationPolicies
{
    public const string RatesRead = "rates.read";
    public const string LabelsWrite = "labels.write";

    public static AuthorizationBuilder AddShippingPolicies(this AuthorizationBuilder builder) =>
        builder
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(RatesRead, policy => policy.RequireScope(RatesRead))
            .AddPolicy(LabelsWrite, policy => policy.RequireScope(LabelsWrite));

    private static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
