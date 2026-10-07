using HarbourLane.Bookings.Web.Content;
using HarbourLane.Bookings.Web.Interop;
using HarbourLane.Bookings.Web.Security;
using HarbourLane.Bookings.Web.State;

namespace HarbourLane.Bookings.Web.Hosting;

internal static class PortalServiceCollectionExtensions
{
    public static IServiceCollection AddBookingPortal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBookingCore();
        services.AddPortalAuthentication(configuration);

        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddRazorPages(options => options.Conventions.AuthorizeFolder("/Admin", AuthorizationPolicies.FacilitiesStaff));
        services.AddCascadingAuthenticationState();

        services.AddSingleton<NoticeSanitizer>();
        services.AddScoped<ToastService>();
        services.AddScoped<ClipboardInterop>();
        services.AddScoped<DialogInterop>();
        services.AddScoped<RoomMapInterop>();

        services.AddHealthChecks();
        services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
        return services;
    }
}
