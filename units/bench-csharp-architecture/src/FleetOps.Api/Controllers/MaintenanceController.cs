using FleetOps.Api.Security;
using FleetOps.Application.Features.Maintenance;
using FleetOps.Contracts.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetOps.Api.Controllers;

[ApiController]
[Route("maintenance")]
public sealed class MaintenanceController(ISender sender) : ControllerBase
{
    [HttpGet("due")]
    [Authorize(Policy = AuthorizationPolicies.FleetRead)]
    public async Task<IReadOnlyList<MaintenanceDue>> Due(CancellationToken cancellationToken) =>
        await sender.Send(new GetDueMaintenanceQuery(), cancellationToken).ConfigureAwait(false);
}
