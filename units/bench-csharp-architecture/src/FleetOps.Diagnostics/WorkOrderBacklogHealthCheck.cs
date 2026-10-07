using System.Globalization;
using FleetOps.Application.Abstractions;
using FleetOps.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FleetOps.Diagnostics;

/// <summary>Degraded when more work orders wait for approval than the workshop can quote in a day.</summary>
public sealed class WorkOrderBacklogHealthCheck(IWorkOrderReadModel workOrders) : IHealthCheck
{
    public const int DegradedAbove = 50;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var waiting = await workOrders.CountAwaitingApprovalAsync(cancellationToken).ConfigureAwait(false);
        var data = new Dictionary<string, object>
        {
            ["awaitingApproval"] = waiting,
            ["contractVersion"] = ContractVersion.Current,
        };
        return waiting > DegradedAbove
            ? HealthCheckResult.Degraded(string.Create(CultureInfo.InvariantCulture, $"{waiting} work orders await approval."), data: data)
            : HealthCheckResult.Healthy(data: data);
    }
}
