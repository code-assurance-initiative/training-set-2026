using FleetOps.Api.Requests;
using FleetOps.Api.Security;
using FleetOps.Application.Features.Inspections;
using FleetOps.Contracts.Inspections;
using FleetOps.Domain.Inspections;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetOps.Api.Controllers;

[ApiController]
[Route("inspections")]
public sealed class InspectionsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.FleetWrite)]
    public async Task<InspectionSummary> Record(RecordInspectionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var defects = request.Defects.Select(d => new Defect(d.Description, d.Severity)).ToList();
        return await sender.Send(new RecordInspectionCommand(request.VehicleId, defects), cancellationToken).ConfigureAwait(false);
    }
}
