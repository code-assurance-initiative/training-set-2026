using FleetOps.Api.Security;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;
using FleetOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Api.Controllers;

/// <summary>The numbers on the fleet office's wall screen.</summary>
[ApiController]
[Route("dashboard")]
public sealed class FleetDashboardController(FleetOpsDbContext db) : ControllerBase
{
    public sealed record DashboardSummary(int ActiveVehicles, int InWorkshop, int OpenWorkOrders, int AwaitingApproval);

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.FleetRead)]
    public async Task<DashboardSummary> Get(CancellationToken cancellationToken)
    {
        var vehicles = db.Vehicles.AsNoTracking();
        var workOrders = db.WorkOrders.AsNoTracking();
        return new DashboardSummary(
            await vehicles.CountAsync(v => v.Status == VehicleStatus.Active, cancellationToken).ConfigureAwait(false),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.InWorkshop, cancellationToken).ConfigureAwait(false),
            await workOrders.CountAsync(w => w.Status != WorkOrderStatus.Completed, cancellationToken).ConfigureAwait(false),
            await workOrders.CountAsync(w => w.Status == WorkOrderStatus.Quoted, cancellationToken).ConfigureAwait(false));
    }
}
