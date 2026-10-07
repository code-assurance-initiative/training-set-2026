using Microsoft.AspNetCore.Authorization;

namespace Warehouse.Stock.Api.Security;

/// <summary>The named policies, each granted by one OAuth scope in the caller's access token.</summary>
public static class AuthorizationPolicies
{
    public const string StockRead = "stock.read";
    public const string StockWrite = "stock.write";
    public const string ReservationsWrite = "reservations.write";

    public static AuthorizationBuilder AddStockPolicies(this AuthorizationBuilder builder) =>
        builder
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(StockRead, policy => policy.RequireScope(StockRead))
            .AddPolicy(StockWrite, policy => policy.RequireScope(StockWrite))
            .AddPolicy(ReservationsWrite, policy => policy.RequireScope(ReservationsWrite));

    private static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
