using Microsoft.AspNetCore.Authorization;

namespace ReportDesk.Api.Security;

/// <summary>The named policies, each granted by one OAuth scope in the caller's access token.</summary>
public static class AuthorizationPolicies
{
    public const string DocumentsRead = "documents.read";
    public const string DocumentsWrite = "documents.write";
    public const string ReportsRead = "reports.read";
    public const string ReportsWrite = "reports.write";
    public const string Administer = "reportdesk.admin";

    public static AuthorizationBuilder AddReportDeskPolicies(this AuthorizationBuilder builder) =>
        builder
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(DocumentsRead, policy => policy.RequireScope(DocumentsRead))
            .AddPolicy(DocumentsWrite, policy => policy.RequireScope(DocumentsWrite))
            .AddPolicy(ReportsRead, policy => policy.RequireScope(ReportsRead))
            .AddPolicy(ReportsWrite, policy => policy.RequireScope(ReportsWrite))
            .AddPolicy(Administer, policy => policy.RequireScope(Administer));

    private static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
