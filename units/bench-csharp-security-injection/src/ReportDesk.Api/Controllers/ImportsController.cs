using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Importing;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("imports")]
[Authorize(Policy = AuthorizationPolicies.DocumentsWrite)]
public sealed class ImportsController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    private const long MaxBundleBytes = 50 * 1024 * 1024;

    /// <summary>Shows what an import by URL would bring in, before the user confirms it.</summary>
    [HttpGet("preview")]
    public async Task<ActionResult<ImportPreview>> Preview(
        [FromQuery, Required, StringLength(2048)] string sourceUrl, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(ImportClient.Name);
        using var response = await client.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var headers = response.Content.Headers;
        return Ok(new ImportPreview(sourceUrl, (int)response.StatusCode, headers.ContentType?.MediaType, headers.ContentLength, headers.LastModified));
    }

    /// <summary>Imports a bundle exported by the desktop client.</summary>
    [HttpPost("bundles")]
    [RequestSizeLimit(MaxBundleBytes)]
    public async Task<ActionResult<IReadOnlyList<string>>> ImportBundle(IFormFile bundle, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        var stream = bundle.OpenReadStream();
        await using (stream.ConfigureAwait(false))
        {
            try
            {
                var entries = BundleReader.ReadEntries(stream);
                return Ok(entries.Select(entry => entry.Name).ToList());
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
            {
                return ValidationProblem(new ValidationProblemDetails { Detail = "The file is not a valid bundle." });
            }
        }
    }
}
