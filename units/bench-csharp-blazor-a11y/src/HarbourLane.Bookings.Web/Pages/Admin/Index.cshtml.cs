using System.Globalization;
using HarbourLane.Bookings.Rooms;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HarbourLane.Bookings.Web.Pages.Admin;

public sealed class IndexModel(IRoomCatalog catalog) : PageModel
{
    internal static readonly CultureInfo Pounds = CultureInfo.GetCultureInfo("en-GB");

    public IReadOnlyList<Room> Rooms { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Rooms = await catalog.GetAllAsync(cancellationToken);
    }
}
