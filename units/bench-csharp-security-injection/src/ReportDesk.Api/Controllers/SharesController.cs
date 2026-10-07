using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Security;
using ReportDesk.Api.Shares;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("shares")]
[Authorize(Policy = AuthorizationPolicies.DocumentsWrite)]
public sealed class SharesController(IShareStore shares, TimeProvider clock) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ShareCreated>> Create(CreateShareRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_');
        var link = new ShareLink(
            token,
            request.DocumentId,
            SharePasswordHasher.Hash(request.Password),
            User.Subject(),
            clock.GetUtcNow().AddDays(request.ValidDays));
        await shares.AddAsync(link, cancellationToken).ConfigureAwait(false);
        return Ok(new ShareCreated(token, link.ExpiresAt));
    }

    /// <summary>The landing form readers outside the archive post their share password to.</summary>
    [HttpPost("{token}/open")]
    [AllowAnonymous]
    public async Task<IActionResult> Open(string token, [FromForm] OpenShareRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var link = await shares.FindAsync(token, cancellationToken).ConfigureAwait(false);
        if (link is null || link.ExpiresAt < clock.GetUtcNow() || !SharePasswordHasher.Verify(request.Password, link.PasswordHash))
        {
            return Unauthorized();
        }

        var returnUrl = request.ReturnUrl;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return LocalRedirect($"/shared/{link.DocumentId:D}");
    }
}
