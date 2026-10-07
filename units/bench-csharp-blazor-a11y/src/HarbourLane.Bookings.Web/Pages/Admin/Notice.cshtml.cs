using HarbourLane.Bookings.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HarbourLane.Bookings.Web.Pages.Admin;

public sealed class NoticeModel(ISiteNoticeStore notices, ILogger<NoticeModel> logger) : PageModel
{
    [BindProperty]
    public string Html { get; set; } = string.Empty;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Html = (await notices.GetAsync(cancellationToken)).Html;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await notices.SetAsync(Html ?? string.Empty, cancellationToken);
        AdminLog.NoticeUpdated(logger, User.Identity?.Name);
        return RedirectToPage("/Admin/Index");
    }
}
