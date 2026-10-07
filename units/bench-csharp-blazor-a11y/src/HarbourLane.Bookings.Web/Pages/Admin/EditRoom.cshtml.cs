using System.ComponentModel.DataAnnotations;
using HarbourLane.Bookings.Rooms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HarbourLane.Bookings.Web.Pages.Admin;

public sealed class EditRoomModel(IRoomCatalog catalog, ILogger<EditRoomModel> logger) : PageModel
{
    [BindProperty]
    public RoomInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var room = await catalog.FindAsync(id, cancellationToken);
        if (room is null)
        {
            return NotFound();
        }

        Input = new RoomInput
        {
            Name = room.Name,
            Capacity = room.Capacity,
            HourlyRate = room.HourlyRate,
            DescriptionHtml = room.DescriptionHtml,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var update = new RoomUpdate(id, Input.Name.Trim(), Input.Capacity, Input.HourlyRate, Input.DescriptionHtml);
        if (!await catalog.UpdateAsync(update, cancellationToken))
        {
            return NotFound();
        }

        AdminLog.RoomUpdated(logger, id, User.Identity?.Name);
        return RedirectToPage("/Admin/Index");
    }

    public sealed class RoomInput
    {
        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 500)]
        public int Capacity { get; set; }

        [Range(typeof(decimal), "0", "500")]
        public decimal HourlyRate { get; set; }

        [Required, StringLength(4000)]
        public string DescriptionHtml { get; set; } = string.Empty;
    }
}
