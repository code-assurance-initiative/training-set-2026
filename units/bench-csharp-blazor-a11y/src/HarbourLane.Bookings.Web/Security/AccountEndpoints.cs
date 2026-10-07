using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;

namespace HarbourLane.Bookings.Web.Security;

internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var account = endpoints.MapGroup("/account");

        account.MapGet("/login", (string? returnUrl) =>
            TypedResults.Challenge(new AuthenticationProperties { RedirectUri = LocalOnly(returnUrl) }))
            .AllowAnonymous();

        account.MapPost("/logout", () =>
            TypedResults.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
            .RequireAuthorization()
            .WithMetadata(new RequireAntiforgeryTokenAttribute());

        return endpoints;
    }

    internal static string LocalOnly(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/";
}
