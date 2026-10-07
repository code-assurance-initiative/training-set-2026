using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Attachments;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("documents/{documentId:guid}/attachments")]
[Authorize(Policy = AuthorizationPolicies.DocumentsRead)]
public sealed class AttachmentsController(AttachmentStore attachments) : ControllerBase
{
    [HttpGet("{fileName}")]
    public async Task<IActionResult> Download(Guid documentId, string fileName, CancellationToken cancellationToken)
    {
        var content = await attachments.ReadAsync(documentId, fileName, cancellationToken).ConfigureAwait(false);
        return content is null ? NotFound() : File(content, "application/octet-stream", fileName);
    }
}
