using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ReportDesk.Api.Http;
using ReportDesk.Api.SavedSearches;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("saved-searches")]
[Authorize(Policy = AuthorizationPolicies.DocumentsWrite)]
public sealed class SavedSearchesController(ISavedSearchStore store) : ControllerBase
{
    /// <summary>Imports a saved-search file exported from another archive (or by a colleague).</summary>
    [HttpPost("import")]
    public async Task<ActionResult<int>> Import(CancellationToken cancellationToken)
    {
        var file = await RequestText.ReadAsync(Request, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<SavedSearchDefinition> searches;
        try
        {
            searches = SavedSearchSerializer.Import(file);
        }
        catch (JsonException ex)
        {
            return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
        }

        var owner = User.Subject();
        foreach (var search in searches)
        {
            await store.SaveAsync(owner, search, cancellationToken).ConfigureAwait(false);
        }

        return Ok(searches.Count);
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, CancellationToken cancellationToken)
    {
        var deleted = await store.DeleteAsync(User.Subject(), name, cancellationToken).ConfigureAwait(false);
        return deleted == 0 ? NotFound() : NoContent();
    }
}
