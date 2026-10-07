using System.Security.Claims;

namespace ReportDesk.Api.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The caller's subject identifier (the <c>sub</c> claim), which owns saved searches and shares.</summary>
    public static string Subject(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirst("sub")?.Value ?? throw new InvalidOperationException("The access token has no subject.");
    }
}
