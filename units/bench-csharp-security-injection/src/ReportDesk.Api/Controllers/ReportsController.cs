using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ReportDesk.Api.Conversion;
using ReportDesk.Api.Hosting;
using ReportDesk.Api.Http;
using ReportDesk.Api.Reports;
using ReportDesk.Api.Templates;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("reports")]
[Authorize(Policy = Security.AuthorizationPolicies.ReportsRead)]
public sealed class ReportsController(
    IReportRepository reports,
    IScheduleRepository schedules,
    TemplateStore templates,
    DocumentConverter converter,
    ThumbnailRenderer thumbnails,
    IOptions<StorageOptions> storage,
    TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReportDefinition>>> ForOwner(
        [FromQuery, Required, StringLength(100)] string owner, CancellationToken cancellationToken) =>
        Ok(await reports.ForOwnerAsync(owner, cancellationToken).ConfigureAwait(false));

    [HttpGet("schedules/due")]
    public async Task<ActionResult<IReadOnlyList<ReportSchedule>>> DueSchedules(
        [FromQuery, Required, StringLength(100)] string owner, CancellationToken cancellationToken) =>
        Ok(await schedules.DueForOwnerAsync(owner, clock.GetUtcNow(), cancellationToken).ConfigureAwait(false));

    [HttpPut("{id:guid}/layout")]
    [Authorize(Policy = Security.AuthorizationPolicies.ReportsWrite)]
    public async Task<IActionResult> SaveLayout(Guid id, CancellationToken cancellationToken)
    {
        var json = await RequestText.ReadAsync(Request, cancellationToken).ConfigureAwait(false);
        ReportLayout layout;
        try
        {
            layout = ReportLayoutSerializer.Deserialize(json);
        }
        catch (JsonException ex)
        {
            return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
        }

        await reports.SaveLayoutAsync(id, ReportLayoutSerializer.Serialize(layout), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{id:guid}/exports")]
    [Authorize(Policy = Security.AuthorizationPolicies.ReportsWrite)]
    public async Task<IActionResult> Export(Guid id, [FromQuery] ExportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var report = await reports.GetAsync(id, cancellationToken).ConfigureAwait(false);
        var template = report is null ? null : await templates.ReadAsync(report.TemplateName, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return NotFound();
        }

        var sourcePath = Path.Combine(storage.Value.ScratchRoot, $"{id:N}.html");
        await System.IO.File.WriteAllTextAsync(sourcePath, template, cancellationToken).ConfigureAwait(false);
        var exported = await converter.ConvertAsync(sourcePath, request.Format, cancellationToken).ConfigureAwait(false);
        await thumbnails.RenderAsync(exported, ThumbnailSize.Small, cancellationToken).ConfigureAwait(false);

        var returnUrl = request.ReturnUrl;
        if (string.IsNullOrEmpty(returnUrl))
        {
            return Accepted();
        }

        return Redirect(returnUrl);
    }
}
