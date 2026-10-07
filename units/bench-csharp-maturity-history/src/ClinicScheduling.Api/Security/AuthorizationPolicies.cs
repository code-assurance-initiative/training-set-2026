using Microsoft.AspNetCore.Authorization;

namespace ClinicScheduling.Api.Security;

/// <summary>The named policies, each granted by one OAuth scope in the caller's access token.</summary>
public static class AuthorizationPolicies
{
    public const string AppointmentsRead = "appointments.read";
    public const string AppointmentsWrite = "appointments.write";
    public const string SchedulesWrite = "schedules.write";
    public const string ReportsRead = "reports.read";
    public const string PortalRead = "portal.read";

    public static AuthorizationBuilder AddSchedulingPolicies(this AuthorizationBuilder builder) =>
        builder
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AppointmentsRead, policy => policy.RequireScope(AppointmentsRead))
            .AddPolicy(AppointmentsWrite, policy => policy.RequireScope(AppointmentsWrite))
            .AddPolicy(SchedulesWrite, policy => policy.RequireScope(SchedulesWrite))
            .AddPolicy(ReportsRead, policy => policy.RequireScope(ReportsRead))
            .AddPolicy(PortalRead, policy => policy.RequireScope(PortalRead));

    private static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policy, string scope) =>
        policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));
}
