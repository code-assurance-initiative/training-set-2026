using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.People;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("people")]
[Authorize(Policy = AuthorizationPolicies.DocumentsRead)]
public sealed class PeopleController(OwnerDirectory owners, GroupDirectory groups) : ControllerBase
{
    [HttpGet("owners")]
    public async Task<ActionResult<IReadOnlyList<PersonEntry>>> Owners(
        [FromQuery, Required, StringLength(254)] string email, CancellationToken cancellationToken) =>
        Ok(await owners.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false));

    [HttpGet("groups/{name}")]
    public async Task<ActionResult<GroupEntry>> Group([StringLength(128)] string name, CancellationToken cancellationToken)
    {
        var group = await groups.FindAsync(name, cancellationToken).ConfigureAwait(false);
        return group is null ? NotFound() : Ok(group);
    }
}
