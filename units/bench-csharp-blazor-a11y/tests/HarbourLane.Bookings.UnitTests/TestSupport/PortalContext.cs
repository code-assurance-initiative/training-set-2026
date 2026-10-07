using System.Security.Claims;
using HarbourLane.Bookings.Web.Content;
using HarbourLane.Bookings.Web.Interop;
using HarbourLane.Bookings.Web.Security;
using HarbourLane.Bookings.Web.State;
using Microsoft.Extensions.DependencyInjection;

namespace HarbourLane.Bookings.UnitTests.TestSupport;

/// <summary>A bUnit context with the portal's services, a fixed clock and a signed-in member.</summary>
public abstract class PortalContext : BunitContext
{
    public const string MemberEmail = "priya@harbourlane-choir.org";

    protected PortalContext()
    {
        Services.AddSingleton<TimeProvider>(FixedClock.AtMondayMorning());
        Services.AddBookingCore();
        Services.AddSingleton<NoticeSanitizer>();
        Services.AddScoped<ToastService>();
        Services.AddScoped<ClipboardInterop>();
        Services.AddScoped<DialogInterop>();
        Services.AddScoped<RoomMapInterop>();

        var auth = AddAuthorization();
        auth.SetAuthorized("Priya Natarajan");
        auth.SetClaims(new Claim("email", MemberEmail), new Claim("sub", "member-42"), new Claim("name", "Priya Natarajan"));
        auth.SetPolicies(AuthorizationPolicies.Member);
    }
}
