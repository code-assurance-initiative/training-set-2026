using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace HarbourLane.Bookings.Web.Security;

internal static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddPortalAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var oidc = configuration.GetSection("Authentication:Oidc");

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = oidc["Authority"];
                options.ClientId = oidc["ClientId"];
                options.ClientSecret = oidc["ClientSecret"];
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = false;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";
                options.Scope.Add("email");
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Member, policy => policy.RequireAuthenticatedUser().RequireClaim("email"))
            .AddPolicy(AuthorizationPolicies.FacilitiesStaff, policy => policy.RequireRole(AuthorizationPolicies.StaffRole));

        return services;
    }
}
