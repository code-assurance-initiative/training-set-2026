using Microsoft.AspNetCore.Authorization;

namespace Fx.Conversion.Api.Security;

/// <summary>The named policies, each granted by one OAuth scope in the caller's access token.</summary>
public static class AuthorizationPolicies
{
    public const string RatesRead = "fx.rates.read";
    public const string Convert = "fx.convert";
    public const string Quote = "fx.quote";

    public static AuthorizationBuilder AddConversionPolicies(this AuthorizationBuilder builder) =>
        builder
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(RatesRead, policy => policy.RequireScope(RatesRead))
            .AddPolicy(Convert, policy => policy.RequireScope(Convert))
            .AddPolicy(Quote, policy => policy.RequireScope(Quote));

    private static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
