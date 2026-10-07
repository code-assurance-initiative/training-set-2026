using Microsoft.AspNetCore.Authorization;

namespace Invoicing.Api.Security;

/// <summary>The named policies, each granted by one OAuth scope in the caller's access token.</summary>
public static class AuthorizationPolicies
{
    public const string RenderInvoices = "invoices.render";
    public const string SubmitErpInvoices = "erp.submit";

    public static AuthorizationBuilder AddInvoicingPolicies(this AuthorizationBuilder builder) =>
        builder
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(RenderInvoices, policy => policy.RequireScope(RenderInvoices))
            .AddPolicy(SubmitErpInvoices, policy => policy.RequireScope(SubmitErpInvoices));

    private static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
