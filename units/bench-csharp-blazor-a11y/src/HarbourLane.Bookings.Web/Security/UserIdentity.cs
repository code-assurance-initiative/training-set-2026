using System.Security.Claims;

namespace HarbourLane.Bookings.Web.Security;

internal static class UserIdentity
{
    public static string? Email(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirst("email")?.Value;
    }

    public static string? Subject(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirst("sub")?.Value;
    }

    public static string DisplayName(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirst("name")?.Value ?? Email(user) ?? string.Empty;
    }
}
